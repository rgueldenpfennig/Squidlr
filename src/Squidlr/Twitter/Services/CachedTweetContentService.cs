using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Squidlr.Twitter.Services;

public sealed class CachedTweetContentService : TweetContentService
{
    private readonly IMemoryCache _memoryCache;

    public CachedTweetContentService(TweetContentParserFactory tweetContentParserFactory, IMemoryCache memoryCache, ILogger<TweetContentService> logger)
        : base(tweetContentParserFactory, logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    public override async ValueTask<Result<TwitterContent, RequestContentResult>> GetTweetContentAsync(TweetIdentifier identifier, CancellationToken cancellationToken)
    {
        var cacheKey = $"TweetContent-{identifier.Id}";
        if (_memoryCache.TryGetValue<Result<TwitterContent, RequestContentResult>>(cacheKey, out var result))
            return result;

        result = await base.GetTweetContentAsync(identifier, cancellationToken);
        if (result.Error is RequestContentResult.Canceled or RequestContentResult.GatewayError)
            return result;

        _memoryCache.Set(cacheKey, result, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(15));
        return result;
    }
}
