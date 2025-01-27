using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;
using System.Xml.Linq;
using DotNext;
using Microsoft.Extensions.Logging;
using Squidlr.Common;
using Squidlr.Twitter.Utilities;

namespace Squidlr.Twitter;

public sealed class TweetContentParser
{
    private readonly TweetIdentifier _tweetIdentifier;
    private readonly TwitterWebClient _twitterClient;
    private readonly ILogger<TweetContentParser> _logger;

    private readonly TwitterContent _tweetContent;
    private JsonElement? _contentElement;
    private JsonElement _legacyElement;
    private JsonElement _extendedEntitiesElement;
    private JsonElement _cardElement;

    public TweetContentParser(TweetIdentifier tweetIdentifier, TwitterWebClient twitterClient, ILogger<TweetContentParser> logger)
    {
        _tweetIdentifier = tweetIdentifier;
        _twitterClient = twitterClient ?? throw new ArgumentNullException(nameof(twitterClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _tweetContent = new TwitterContent(tweetIdentifier);
    }

    public async Task<Result<TwitterContent, RequestContentResult>> CreateTweetContentAsync(CancellationToken cancellationToken)
    {
        RequestContentResult result;

        try
        {
            result = await TryInitializeTweetContentAsync(_tweetIdentifier, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unable to parse Tweet detail contents.");
            return new(RequestContentResult.Error);
        }

        if (result == RequestContentResult.Success && _tweetContent.Videos.Count > 0)
        {
            return _tweetContent;
        }
        else if (result == RequestContentResult.Success && _tweetContent.Videos.Count == 0)
        {
            return new(RequestContentResult.NoVideo);
        }

        return new(result);
    }

    private async ValueTask<RequestContentResult> TryInitializeTweetContentAsync(TweetIdentifier tweetIdentifier, CancellationToken cancellationToken)
    {
        var tweetDetailResult = await _twitterClient.GetTweetDetailStreamAsync(tweetIdentifier, cancellationToken);
        if (!tweetDetailResult.IsSuccessful)
        {
            return tweetDetailResult.Error;
        }

        using var document = JsonDocument.Parse(tweetDetailResult.Value);
        var root = document.RootElement;

        _contentElement = root.GetPropertyOrNull("data")?
                             .GetPropertyOrNull("tweetResult")?
                             .GetPropertyOrNull("result");

        if (_contentElement == null)
        {
            _logger.LogInformation("Could not find 'result' element in Tweet detail JSON contents. Tweet seems not to be found.");
            return RequestContentResult.NotFound;
        }

        if (_contentElement.Value.TryGetProperty("tweet", out var tweetElement))
        {
            _contentElement = tweetElement;
        }

        var reason = _contentElement.Value.GetPropertyOrNull("reason")?.GetString();
        if (reason != null)
        {
            _logger.LogInformation("Unable to process Tweet due to reason '{Reason}'", reason);

            return reason switch
            {
                "Suspended" => RequestContentResult.AccountSuspended,
                "NsfwLoggedOut" => RequestContentResult.AdultContent,
                "Protected" => RequestContentResult.Protected,
                "BounceDeleted" => RequestContentResult.NoVideo,
                _ => RequestContentResult.Error
            };
        }

        var restId = _contentElement.Value.GetPropertyOrNull("rest_id")?.GetString();
        if (!tweetIdentifier.Id.Equals(restId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Tweet ID does not match 'rest_id'.");
            return RequestContentResult.Error;
        }

        _legacyElement = _contentElement.Value.GetProperty("legacy");
        SetCoreTweetContentValues();

        if (_legacyElement.TryGetProperty("extended_entities", out _extendedEntitiesElement))
        {
            // this matches most Tweets with directly embedded videos
            await CreateFromExtendedEntitiesAsync(cancellationToken);
            return RequestContentResult.Success;
        }

        // the requested Tweet contains a quoted Tweet
        if (_contentElement.Value.TryGetProperty("quoted_status_result", out var quotedResult))
        {
            if (quotedResult.GetPropertyOrNull("result")?.GetPropertyOrNull("legacy")?.TryGetProperty("extended_entities", out _extendedEntitiesElement) ?? false)
            {
                await CreateFromExtendedEntitiesAsync(cancellationToken);
                return RequestContentResult.Success;
            }
        }

        // the requested Tweet contains a quoted Tweet
        if (_legacyElement.TryGetProperty("quoted_status_permalink", out var quotedStatus))
        {
            var quotedStatusUrl = quotedStatus.GetProperty("expanded").GetString()!;
            var quotedStatusId = UrlUtilities.CreateTweetIdentifierFromUrl(quotedStatusUrl);

            return await TryInitializeTweetContentAsync(quotedStatusId, cancellationToken);
        }

        // the requested Tweet contains a quoted Tweet
        var urls = _legacyElement.GetPropertyOrNull("entities")?.GetPropertyOrNull("urls");
        if (urls != null)
        {
            foreach (var url in urls.Value.EnumerateArray())
            {
                var expandedUrl = url.GetProperty("expanded_url").GetString()!;
                if (UrlUtilities.IsValidTwitterStatusUrl(expandedUrl))
                {
                    var expandedUrlId = UrlUtilities.CreateTweetIdentifierFromUrl(expandedUrl);
                    return await TryInitializeTweetContentAsync(expandedUrlId, cancellationToken);
                }
            }
        }

        // the requested Tweet contains a quoted Tweet
        if (_legacyElement.TryGetProperty("retweeted_status_result", out var retweetedStatusResult))
        {
            if (retweetedStatusResult.GetProperty("result")
                                     .GetProperty("legacy")
                                     .TryGetProperty("extended_entities", out _extendedEntitiesElement))
            {
                await CreateFromExtendedEntitiesAsync(cancellationToken);
                return RequestContentResult.Success;
            }
        }

        if (_contentElement.Value.TryGetProperty("card", out _cardElement))
        {
            // matches card Tweets for example polls with integrated videos
            var nameElement = _cardElement.GetPropertyOrNull("legacy")?.GetPropertyOrNull("name");
            if (nameElement == null)
            {
                _logger.LogWarning("Could not find 'name' from card in Tweet detail contents.");
                return RequestContentResult.Error;
            }

            var name = nameElement.Value.GetString();
            var tweetCardType = name switch
            {
                string val when val.Contains("poll2choice_video", StringComparison.OrdinalIgnoreCase) => TwitterCardType.Poll2ChoiceVideo,
                string val when val.Contains("poll3choice_video", StringComparison.OrdinalIgnoreCase) => TwitterCardType.Poll3ChoiceVideo,
                string val when val.Contains("poll4choice_video", StringComparison.OrdinalIgnoreCase) => TwitterCardType.Poll4ChoiceVideo,
                string val when val.Contains("broadcast", StringComparison.OrdinalIgnoreCase) => TwitterCardType.Broadcast,
                string val when val.Contains("video_direct_message", StringComparison.OrdinalIgnoreCase) => TwitterCardType.VideoDirectMessage,
                string val when val.Equals("amplify", StringComparison.OrdinalIgnoreCase) => TwitterCardType.Amplify,
                string val when val.Equals("promo_video_convo", StringComparison.OrdinalIgnoreCase) => TwitterCardType.PromoVideo,
                string val when val.Equals("unified_card", StringComparison.OrdinalIgnoreCase) => TwitterCardType.UnifiedCard,
                _ => TwitterCardType.Unknown
            };

            switch (tweetCardType)
            {
                case TwitterCardType.Unknown:
                case TwitterCardType.Broadcast:
                    _logger.LogInformation("Could not fulfill unsupported video request of card type '{TweetCardType}'.", tweetCardType);
                    return RequestContentResult.UnsupportedVideo;
                case TwitterCardType.Poll2ChoiceVideo:
                case TwitterCardType.Poll3ChoiceVideo:
                case TwitterCardType.Poll4ChoiceVideo:
                    return await CreateFromCardWithVmapAsync("player_stream_url", cancellationToken);
                case TwitterCardType.Amplify:
                    return await CreateFromCardWithVmapAsync("amplify_url_vmap", cancellationToken);
                case TwitterCardType.PromoVideo:
                    return await CreateFromCardWithVmapAsync("player_url", cancellationToken);
                case TwitterCardType.UnifiedCard:
                    return await CreateFromUnifiedCardAsync(cancellationToken);
                case TwitterCardType.VideoDirectMessage:
                    return await CreateFromCardWithVmapAsync("player_url", cancellationToken);
                default:
                    return RequestContentResult.NoVideo;
            }
        }

        return RequestContentResult.NoVideo;
    }

    private async ValueTask CreateFromExtendedEntitiesAsync(CancellationToken cancellationToken)
    {
        if (_contentElement!.Value.TryGetProperty("views", out var views) &&
            views.TryGetProperty("count", out var count))
        {
            _tweetContent.Views = Convert.ToInt32(count.GetString());
        }

        var mediaArray = _extendedEntitiesElement.GetProperty("media");
        await foreach (var tweetMediaVideo in ExtractVideosFromMediaArrayAsync(mediaArray, cancellationToken))
            if (tweetMediaVideo != null)
                _tweetContent.AddVideo(tweetMediaVideo);
    }

    private async ValueTask<RequestContentResult> CreateFromUnifiedCardAsync(CancellationToken cancellationToken)
    {
        var bindingValues = _cardElement.GetPropertyOrNull("legacy")?.GetPropertyOrNull("binding_values");
        if (bindingValues == null)
        {
            _logger.LogWarning("Could not find 'binding_values' in Tweet detail contents of a card.");
            return RequestContentResult.Error;
        }

        var unifiedCardElement = GetValueByKey(bindingValues.Value, "unified_card");
        if (unifiedCardElement == null)
        {
            _logger.LogWarning("Could not find 'unified_card' in Tweet detail contents of a card.");
            return RequestContentResult.Error;
        }

        var unifiedJson = unifiedCardElement.Value.GetProperty("string_value").GetString()!;
        var document = JsonDocument.Parse(unifiedJson);

        var mediaEntitiesElement = document.RootElement.GetProperty("media_entities");
        foreach (var mediaEntity in mediaEntitiesElement.EnumerateObject())
        {
            var tweetMediaVideo = await ExtractVideoFromMediaEntityAsync(mediaEntity.Value, cancellationToken);
            if (tweetMediaVideo != null)
                _tweetContent.AddVideo(tweetMediaVideo);
        }

        return RequestContentResult.Success;
    }

    private async ValueTask<RequestContentResult> CreateFromCardWithVmapAsync(string vmapPropertyName, CancellationToken cancellationToken)
    {
        var bindingValues = _cardElement.GetPropertyOrNull("legacy")?.GetPropertyOrNull("binding_values");
        if (bindingValues == null)
        {
            _logger.LogWarning("Could not find 'binding_values' in Tweet detail contents of a card.");
            return RequestContentResult.Error;
        }

        var playerStreamUrlElement = GetValueByKey(bindingValues.Value, vmapPropertyName);
        if (playerStreamUrlElement == null)
        {
            _logger.LogWarning("Could not find {vmapPropertyName} in Tweet detail contents of a card.", vmapPropertyName);
            return RequestContentResult.Error;
        }

        var playerImageElement = GetValueByKey(bindingValues.Value, "player_image");
        if (playerImageElement == null)
        {
            _logger.LogWarning("Could not find 'player_image' in Tweet detail contents of a card.");
            return RequestContentResult.Error;
        }

        var contentDurationElement = GetValueByKey(bindingValues.Value, "content_duration_seconds");
        if (contentDurationElement == null)
        {
            _logger.LogWarning("Could not find 'content_duration_seconds' in Tweet detail contents of a card.");
        }

        var video = new Video
        {
            DisplayUrl = new Uri(playerImageElement.Value.GetProperty("image_value").GetProperty("url").GetString()!),
            Duration = contentDurationElement.HasValue ? TimeSpan.FromSeconds(Convert.ToInt32(contentDurationElement.Value.GetProperty("string_value").GetString())) : null
        };

        var playerStreamUrl = playerStreamUrlElement.Value.GetProperty("string_value").GetString()!;
        using var response = await _twitterClient.GetAsync(new Uri(playerStreamUrl, UriKind.Absolute), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Could not request 'player_stream_url'.");
            return RequestContentResult.Error;
        }

        var xmlFileStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var xDoc = await XDocument.LoadAsync(xmlFileStream, LoadOptions.None, cancellationToken);

        XNamespace ns = "http://twitter.com/schema/videoVMapV2.xsd";
        var videoVariants = xDoc.Descendants(ns + "videoVariants").Elements();

        await Parallel.ForEachAsync(videoVariants, cancellationToken, async (videoVariant, ct) =>
        {
            ct.ThrowIfCancellationRequested();

            var contentType = videoVariant.Attribute("content_type")?.Value;
            if (contentType == null || !contentType.Equals("video/mp4", StringComparison.OrdinalIgnoreCase))
                return;

            var videoUrl = videoVariant.Attribute("url")?.Value;
            var bitrateAttribute = videoVariant.Attribute("bit_rate");
            var bitrate = bitrateAttribute != null ? Convert.ToInt32(bitrateAttribute.Value) : 0;

            if (videoUrl != null)
            {
                var decodedVideoUrl = HttpUtility.UrlDecode(videoUrl);
                var uri = new Uri(decodedVideoUrl);
                var contentLength = await _twitterClient.GetVideoContentLengthAsync(uri, ct);

                lock (video)
                {
                    video.VideoSources.Add(new()
                    {
                        Bitrate = bitrate,
                        ContentType = contentType,
                        ContentLength = contentLength,
                        Size = UrlUtilities.ParseSizeFromVideoUrl(decodedVideoUrl),
                        Url = uri
                    });
                }
            }
        });

        _tweetContent.AddVideo(video);

        return RequestContentResult.Success;
    }

    private static JsonElement? GetValueByKey(JsonElement bindingValues, string key)
    {
        foreach (var kvp in bindingValues.EnumerateArray())
        {
            if (kvp.GetProperty("key").GetString() == key)
                return kvp.GetProperty("value");
        }

        return null;
    }

    private void SetCoreTweetContentValues()
    {
        _tweetContent.Source = _contentElement!.Value.GetProperty("source").GetString();
        _tweetContent.CreatedAtUtc = _legacyElement.GetProperty("created_at").GetString()!.ParseToDateTimeOffset();
        _tweetContent.BookmarkCount = _legacyElement.GetProperty("bookmark_count").GetInt32();
        _tweetContent.FavoriteCount = _legacyElement.GetProperty("favorite_count").GetInt32();
        _tweetContent.QuoteCount = _legacyElement.GetProperty("quote_count").GetInt32();
        _tweetContent.ReplyCount = _legacyElement.GetProperty("reply_count").GetInt32();
        _tweetContent.RetweetCount = _legacyElement.GetProperty("retweet_count").GetInt32();
        _tweetContent.FullText = _legacyElement.GetProperty("full_text").GetString();

        var legacyUserResult = _contentElement!.Value.GetPropertyOrNull("core")?
                                                     .GetPropertyOrNull("user_results")?
                                                     .GetPropertyOrNull("result")?
                                                     .GetPropertyOrNull("legacy");
        if (legacyUserResult != null)
        {
            _tweetContent.DisplayName = legacyUserResult.Value.GetPropertyOrNull("name")?.GetString();
            _tweetContent.Username = legacyUserResult.Value.GetPropertyOrNull("screen_name")?.GetString();
            if (legacyUserResult.Value.TryGetProperty("profile_image_url_https", out var profileImageUrl) && profileImageUrl.GetString() != null)
            {
                _tweetContent.ProfileImageUri = new Uri(profileImageUrl.GetString()!, UriKind.Absolute);
            }
        }
    }

    private async IAsyncEnumerable<Video?> ExtractVideosFromMediaArrayAsync(JsonElement mediaArray, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var mediaEntity in mediaArray.EnumerateArray())
        {

            yield return await ExtractVideoFromMediaEntityAsync(mediaEntity, cancellationToken);
        }
    }

    private async ValueTask<Video?> ExtractVideoFromMediaEntityAsync(JsonElement mediaEntity, CancellationToken cancellationToken)
    {
        if (!mediaEntity.TryGetProperty("video_info", out var videoInfo))
            return null;

        var mediaUrlHttps = mediaEntity.GetProperty("media_url_https").GetString()!;
        var video = new Video
        {
            DisplayUrl = new Uri(mediaUrlHttps),
            Monetizable = mediaEntity.GetPropertyOrNull("additional_media_info")?.GetPropertyOrNull("monetizable")?.GetBoolean(),
            Views = mediaEntity.GetPropertyOrNull("mediaStats")?.GetPropertyOrNull("viewCount")?.GetInt32()
        };

        if (videoInfo.TryGetProperty("duration_millis", out var durationProperty))
        {
            video.Duration = TimeSpan.FromMilliseconds(durationProperty.GetInt32());
        }

        var variants = videoInfo.GetProperty("variants").EnumerateArray();
        await Parallel.ForEachAsync(variants, cancellationToken, async (variant, ct) =>
        {
            ct.ThrowIfCancellationRequested();

            var contentType = variant.GetProperty("content_type").GetString();
            if (contentType != null && contentType.Equals("video/mp4", StringComparison.OrdinalIgnoreCase))
            {
                var bitrate = variant.GetProperty("bitrate").GetInt32();
                var videoUrl = variant.GetProperty("url").GetString();

                if (videoUrl != null)
                {
                    var uri = new Uri(videoUrl);
                    var contentLength = await _twitterClient.GetVideoContentLengthAsync(uri, cancellationToken);

                    lock (video)
                    {
                        video.VideoSources.Add(new()
                        {
                            Bitrate = bitrate,
                            ContentType = contentType,
                            ContentLength = contentLength,
                            Size = UrlUtilities.ParseSizeFromVideoUrl(videoUrl),
                            Url = uri
                        });
                    }
                }
            }
        });

        return video;
    }
}
