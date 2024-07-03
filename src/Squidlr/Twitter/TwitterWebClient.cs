using System.Net;
using System.Text.Json;
using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Squidlr.Twitter.Utilities;

namespace Squidlr.Twitter;

public sealed class TwitterWebClient
{
    public const string HttpClientName = nameof(TwitterWebClient);

    private readonly IHttpClientFactory _clientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TwitterWebClient> _logger;

    public TwitterWebClient(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, ILogger<TwitterWebClient> logger)
    {
        _clientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient(HttpClientName);
        return client.GetAsync(requestUri, cancellationToken);
    }

    public async ValueTask<long?> GetVideoContentLengthAsync(Uri videoFileUri, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient(HttpClientName);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Head, videoFileUri);
        using var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return response.Content.Headers.ContentLength;
        }

        return null;
    }

    public async Task<Result<Stream, RequestContentResult>> GetTweetDetailStreamAsync(TweetIdentifier tweet, CancellationToken cancellationToken)
    {
        var guestToken = await GetOrCreateGuestTokenAsync(tweet, cancellationToken);
        if (guestToken == null)
        {
            return new(RequestContentResult.GatewayError);
        }

        var client = _clientFactory.CreateClient(HttpClientName);
        var tweetDetailUrl = $"https://twitter.com/i/api/graphql/2ICDjqPd81tulZcYrtpTuQ/TweetResultByRestId?variables=%7B%22tweetId%22%3A%22{tweet.Id}%22%2C%22withCommunity%22%3Afalse%2C%22includePromotedContent%22%3Afalse%2C%22withVoice%22%3Afalse%7D&features=%7B%22creator_subscriptions_tweet_preview_api_enabled%22%3Atrue%2C%22tweetypie_unmention_optimization_enabled%22%3Atrue%2C%22responsive_web_edit_tweet_api_enabled%22%3Atrue%2C%22graphql_is_translatable_rweb_tweet_is_translatable_enabled%22%3Atrue%2C%22view_counts_everywhere_api_enabled%22%3Atrue%2C%22longform_notetweets_consumption_enabled%22%3Atrue%2C%22responsive_web_twitter_article_tweet_consumption_enabled%22%3Afalse%2C%22tweet_awards_web_tipping_enabled%22%3Afalse%2C%22freedom_of_speech_not_reach_fetch_enabled%22%3Atrue%2C%22standardized_nudges_misinfo%22%3Atrue%2C%22tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled%22%3Atrue%2C%22longform_notetweets_rich_text_read_enabled%22%3Atrue%2C%22longform_notetweets_inline_media_enabled%22%3Atrue%2C%22responsive_web_graphql_exclude_directive_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22responsive_web_media_download_video_enabled%22%3Afalse%2C%22responsive_web_graphql_skip_user_profile_image_extensions_enabled%22%3Afalse%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%2C%22responsive_web_enhance_cards_enabled%22%3Afalse%7D&fieldToggles=%7B%22withArticleRichContentState%22%3Afalse%7D";

        var request = new HttpRequestMessage(HttpMethod.Get, tweetDetailUrl);
        request.Headers.Add("x-guest-token", guestToken);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Received {TweetDetailHttpStatusCode} HTTP status code when trying to request Tweet detail.",
                response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new(RequestContentResult.NotFound);

            return new(RequestContentResult.GatewayError);
        }

        var streamCopy = new MemoryStream(
            response.Content.Headers.ContentLength != null ?
            (int)response.Content.Headers.ContentLength : 32_768);

        await response.Content.CopyToAsync(streamCopy, cancellationToken);
        streamCopy.Position = 0;

        return streamCopy;
    }

    private async ValueTask<string?> GetOrCreateGuestTokenAsync(TweetIdentifier tweet, CancellationToken cancellationToken)
    {
        try
        {
            return await _memoryCache.GetOrCreateAsync("TwitterGuestToken", async (cacheEntry) =>
            {
                string? guestToken;
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);

                var client = _clientFactory.CreateClient(HttpClientName);
                using var response = await client.PostAsync("/1.1/guest/activate.json", content: null, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Twitter guest token API is probably deprecated.");
                    client.DefaultRequestHeaders.Remove("Authorization"); // we must remove the Authorization header in that case
                    var document = await client.GetStringAsync(tweet.Url, cancellationToken);

                    guestToken = GuestTokenUtilities.ExtractGuestToken(document) ??
                        throw new InvalidOperationException("Guest token not found in HTML document");

                    return guestToken;
                }

                using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var json = JsonDocument.Parse(responseStream);

                guestToken = json.RootElement.GetProperty("guest_token").GetString() ??
                    throw new InvalidOperationException("guest_token is null");

                return guestToken;
            });
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Could not request new Twitter guest token.");
            return null;
        }
    }
}
