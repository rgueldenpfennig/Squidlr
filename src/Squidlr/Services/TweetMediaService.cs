using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Squidlr.Client;

namespace Squidlr.Services;

public sealed class TweetMediaService
{
    private readonly TwitterWebClient _client;
    private readonly ILogger<TweetMediaService> _logger;

    public TweetMediaService(TwitterWebClient client, ILogger<TweetMediaService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask CopyTweetVideoStreamAsync(string tweetId, Uri fileUri, HttpContext httpContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Downloading Twitter video...");
        httpContext.Response.Headers["content-disposition"] = $"attachment; fileName=Squidlr-{tweetId}.mp4";

        try
        {
            await _client.CopyFileStreamAsync(fileUri, httpContext, cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex is TaskCanceledException)
                return;

            _logger.LogWarning(ex, "An exception occurred while downloading video file.");
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadGateway;
        }
    }
}
