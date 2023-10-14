using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DotNext;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Squidlr.Twitter;

public sealed class TwitterWebClient
{
    public const string HttpClientName = nameof(TwitterWebClient);

    private const int _defaultBufferSize = 65_536;

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

    public async ValueTask CopyFileStreamAsync(Uri fileUri, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient(HttpClientName);
        var request = new HttpRequestMessage(HttpMethod.Get, fileUri);

        if (httpContext.Request.Headers.TryGetValue("Range", out var rangeHeader))
        {
            request.Headers.Range = RangeHeaderValue.Parse(rangeHeader);
        }

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        httpContext.Response.StatusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength is not null)
        {
            httpContext.Response.ContentLength = response.Content.Headers.ContentLength;
            httpContext.Response.ContentType = response.Content.Headers.ContentType?.ToString();
            if (response.Headers.AcceptRanges?.Count > 0)
            {
                httpContext.Response.Headers.Add("Accept-Ranges", response.Headers.AcceptRanges.ToString());
            }

            using var input = await response.Content.ReadAsStreamAsync();
            await CopyStream(httpContext.Response.Body, input, cancellationToken);
        }
    }

    public async Task<Result<Stream, RequestVideoResult>> GetTweetDetailStreamAsync(TweetIdentifier tweet, CancellationToken cancellationToken)
    {
        var guestToken = await GetOrCreateGuestTokenAsync(cancellationToken);
        if (guestToken == null)
        {
            return new(RequestVideoResult.GatewayError);
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
                return new(RequestVideoResult.NotFound);

            return new(RequestVideoResult.GatewayError);
        }

        var streamCopy = new MemoryStream(
            response.Content.Headers.ContentLength != null ?
            (int)response.Content.Headers.ContentLength : 32_768);

        await response.Content.CopyToAsync(streamCopy, cancellationToken);
        streamCopy.Position = 0;

        return streamCopy;
    }

    private async ValueTask<string?> GetOrCreateGuestTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _memoryCache.GetOrCreateAsync("TwitterGuestToken", async (cacheEntry) =>
            {
                var client = _clientFactory.CreateClient(HttpClientName);
                using var response = await client.PostAsync("/1.1/guest/activate.json", content: null, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var json = JsonDocument.Parse(responseStream);

                var guestToken = json.RootElement.GetProperty("guest_token").GetString();
                if (guestToken == null)
                    throw new InvalidOperationException("guest_token is null");

                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);

                return guestToken;
            });
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Could not request new Twitter guest token.");
            return null;
        }
    }

    private static async ValueTask CopyStream(Stream output, Stream input, CancellationToken cancellationToken)
    {
        // stream copying taken from https://github.com/microsoft/reverse-proxy/blob/main/src/ReverseProxy/Forwarder/StreamCopier.cs
        var buffer = ArrayPool<byte>.Shared.Rent(_defaultBufferSize);
        int read;
        long contentLength = 0;
        try
        {
            while (true)
            {
                read = 0;

                // Issue a zero-byte read to the input stream to defer buffer allocation until data is available.
                // Note that if the underlying stream does not supporting blocking on zero byte reads, then this will
                // complete immediately and won't save any memory, but will still function correctly.
                var zeroByteReadTask = input.ReadAsync(Memory<byte>.Empty, cancellationToken);
                if (zeroByteReadTask.IsCompletedSuccessfully)
                {
                    // Consume the ValueTask's result in case it is backed by an IValueTaskSource
                    _ = zeroByteReadTask.Result;
                }
                else
                {
                    // Take care not to return the same buffer to the pool twice in case zeroByteReadTask throws
                    var bufferToReturn = buffer;
                    buffer = null;
                    ArrayPool<byte>.Shared.Return(bufferToReturn);

                    await zeroByteReadTask;

                    buffer = ArrayPool<byte>.Shared.Rent(_defaultBufferSize);
                }

                read = await input.ReadAsync(buffer.AsMemory(), cancellationToken);
                contentLength += read;

                // End of the source stream.
                if (read == 0)
                {
                    break;
                }

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }
        finally
        {
            if (buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
