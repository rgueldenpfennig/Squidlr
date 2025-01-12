using System;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using DotNext;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Squidlr.Abstractions;
using Squidlr.Facebook.Utilities;

namespace Squidlr.Facebook;

public sealed partial class FacebookContentProvider : IContentProvider
{
    private readonly FacebookWebClient _client;
    private readonly ILogger<FacebookContentProvider> _logger;

    [GeneratedRegex(@"""publish_time\\"":(?<timeStamp>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PublishTimeRegex();

    [GeneratedRegex(@"""unified_reactors"":{""count"":(?<count>\d+)}", RegexOptions.IgnoreCase)]
    private static partial Regex UnifiedReactorsCountRegex();

    [GeneratedRegex(@"""total_comment_count"":(?<count>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex TotalCommentCountRegex();

    [GeneratedRegex(@"""share_count_reduced"":""(?<count>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex ShareCountRegex();

    [GeneratedRegex(@"""__isActor"":""User"",""name"":""(?<name>.*?)""", RegexOptions.IgnoreCase)]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"""browser_native_hd_url"":""(?<url>[^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex NativeHdUrlRegex();

    [GeneratedRegex(@"""browser_native_sd_url"":""(?<url>[^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex NativeSdUrlRegex();

    [GeneratedRegex(@"""first_frame_thumbnail"":""(?<url>[^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex FirstFrameThumbnailRegex();

    [GeneratedRegex(@"""playback_video"":{""height"":(?<height>\d+),""width"":(?<width>\d+),""length_in_second"":(?<seconds>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PlaybackVideoRegex();

    public SocialMediaPlatform Platform { get; } = SocialMediaPlatform.Facebook;

    public FacebookContentProvider(FacebookWebClient client, ILogger<FacebookContentProvider> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, CancellationToken cancellationToken)
    {
        var identifier = UrlUtilities.GetFacebookIdentifier(url);
        using var response = await _client.GetFacebookPostAsync(identifier, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Received {FacebookHttpStatusCode} HTTP status code when trying to request Facebook post.",
                response.StatusCode);

            return response.StatusCode == HttpStatusCode.NotFound ? new(RequestContentResult.NotFound) : new(RequestContentResult.GatewayError);
        }

        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var video = await FindVideoAsync(htmlContent, cancellationToken);
        if (video == null)
        {
            return new(RequestContentResult.NoVideo);
        }

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        var ogDescription = htmlDoc.DocumentNode.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", null);

        var publishTimeMatch = PublishTimeRegex().Match(htmlContent);
        var publishTime = publishTimeMatch.Success ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(publishTimeMatch.Groups["timeStamp"].Value, CultureInfo.InvariantCulture)) : DateTimeOffset.UtcNow;

        var unifiedReactorsMatch = UnifiedReactorsCountRegex().Match(htmlContent);
        var favoriteCount = unifiedReactorsMatch.Success ? int.Parse(unifiedReactorsMatch.Groups["count"].Value, CultureInfo.InvariantCulture) : 0;

        var totalCommentCountMatch = TotalCommentCountRegex().Match(htmlContent);
        var replyCount = totalCommentCountMatch.Success ? int.Parse(totalCommentCountMatch.Groups["count"].Value, CultureInfo.InvariantCulture) : 0;

        var shareCountMatch = ShareCountRegex().Match(htmlContent);
        var shareCount = shareCountMatch.Success ? int.Parse(shareCountMatch.Groups["count"].Value, CultureInfo.InvariantCulture) : 0;

        var usernameMatch = NameRegex().Match(htmlContent);
        var username = usernameMatch.Success ? usernameMatch.Groups["name"].Value : null;

        var content = new FacebookContent(identifier.Url)
        {
            CreatedAtUtc = publishTime,
            FullText = ogDescription,
            FavoriteCount = favoriteCount,
            ReplyCount = replyCount,
            ShareCount = shareCount,
            Username = username
        };

        content.AddVideo(video);

        return content;
    }

    private async ValueTask<Video?> FindVideoAsync(string htmlContent, CancellationToken cancellationToken)
    {
        var hdMatch = NativeHdUrlRegex().Match(htmlContent);
        var sdMatch = NativeSdUrlRegex().Match(htmlContent);

        if (!hdMatch.Success && !sdMatch.Success)
        {
            return null;
        }

        var playbackVideoMatch = PlaybackVideoRegex().Match(htmlContent);
        var duration = playbackVideoMatch.Success ? TimeSpan.FromSeconds(int.Parse(playbackVideoMatch.Groups["seconds"].Value, CultureInfo.InvariantCulture)) : (TimeSpan?)null;
        var height = playbackVideoMatch.Success ? int.Parse(playbackVideoMatch.Groups["height"].Value, CultureInfo.InvariantCulture) : 0;
        var width = playbackVideoMatch.Success ? int.Parse(playbackVideoMatch.Groups["width"].Value, CultureInfo.InvariantCulture) : 0;

        var firstFrameThumbnailMatch = FirstFrameThumbnailRegex().Match(htmlContent);
        var displayUrl = firstFrameThumbnailMatch.Success ? new Uri(firstFrameThumbnailMatch.Groups["url"].Value.Replace("\\", string.Empty), UriKind.Absolute) : null;

        var video = new Video
        {
            Duration = duration,
            DisplayUrl = displayUrl
        };

        var videoSize = VideoSize.Empty;
        if (height > 0 && width > 0)
        {
            videoSize = new VideoSize(height, width);
        }

        if (hdMatch.Success)
        {
            await AddVideoSourceAsync(video, videoSize, new Uri(hdMatch.Groups["url"].Value.Replace("\\", string.Empty), UriKind.Absolute), isSd: false, cancellationToken);
        }

        if (sdMatch.Success)
        {
            await AddVideoSourceAsync(video, videoSize, new Uri(sdMatch.Groups["url"].Value.Replace("\\", string.Empty), UriKind.Absolute), isSd: true, cancellationToken);
        }

        return video;
    }

    private async Task AddVideoSourceAsync(Video video, VideoSize videoSize, Uri uri, bool isSd, CancellationToken cancellationToken)
    {
        var (contentLength, mediaType) = await _client.GetVideoContentLengthAndMediaTypeAsync(uri, cancellationToken);

        video.AddVideoSource(new()
        {
            Bitrate = 0,
            ContentLength = contentLength,
            ContentType = mediaType ?? "video/mp4",
            Size = isSd ? new VideoSize(videoSize.Height / 2, videoSize.Width / 2) : videoSize,
            Url = uri
        });
    }
}
