using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Squidlr.Parser;

namespace Squidlr.Services;

public sealed class CachedTweetContentService : TweetContentService
{
    private readonly IMemoryCache _memoryCache;

    public CachedTweetContentService(TweetContentParserFactory tweetContentParserFactory, IMemoryCache memoryCache, ILogger<TweetContentService> logger)
        : base(tweetContentParserFactory, logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    public override async ValueTask<Result<TweetContent, GetTweetVideoResult>> GetTweetContentAsync(TweetIdentifier identifier, CancellationToken cancellationToken)
    {
        var cacheKey = $"TweetContent-{identifier.Id}";
        if (_memoryCache.TryGetValue<Result<TweetContent, GetTweetVideoResult>>(cacheKey, out var result))
            return result;

        result = await base.GetTweetContentAsync(identifier, cancellationToken);
        if (result.Error is GetTweetVideoResult.Canceled or GetTweetVideoResult.GatewayError) return result;

        _memoryCache.Set(cacheKey, result, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(15));
        return result;
    }
}
