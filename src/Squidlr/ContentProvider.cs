using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Squidlr.Abstractions;

namespace Squidlr;

public sealed class ContentProvider
{
    private readonly IReadOnlyList<IContentProvider> _contentProviders;

    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ContentProvider> _logger;

    private static readonly Result<Content, RequestContentResult> _platformNotSupportedResult = new (RequestContentResult.PlatformNotSupported);

    public ContentProvider(IReadOnlyList<IContentProvider> contentProviders, IMemoryCache memoryCache, ILogger<ContentProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(contentProviders);
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(logger);
        if (contentProviders.Count == 0)
        {
            throw new ArgumentException($"No {nameof(IContentProvider)} instances have been provided.", nameof(contentProviders));
        }

        _contentProviders = contentProviders;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async ValueTask<Result<Content, RequestContentResult>> GetContentAsync(ContentIdentifier contentIdentifier, CancellationToken cancellationToken)
    {
        var cacheKey = $"{contentIdentifier.Platform}-{contentIdentifier.Id}";
        if (_memoryCache.TryGetValue<Result<Content, RequestContentResult>>(cacheKey, out var result))
        {
            _logger.LogInformation(
                "Cache hit for {ContentId} at {ContentUrl} on {SocialMediaPlatform}",
                contentIdentifier.Id, contentIdentifier.Url, contentIdentifier.Platform);

            return result;
        }

        for (var i = 0; i < _contentProviders.Count; i++)
        {
            var provider = _contentProviders[i];
            if (provider.Platform == contentIdentifier.Platform)
            {
                try
                {
                    _logger.LogInformation(
                        "Loading content from {SocialMediaPlatform}: {ContentId} at {ContentUrl}",
                        contentIdentifier.Platform, contentIdentifier.Id, contentIdentifier.Url);

                    var content = await provider.GetContentAsync(contentIdentifier.Url, cancellationToken);
                    if (content.Error == RequestContentResult.Success || content.Error == RequestContentResult.NotFound)
                    {
                        _memoryCache.Set(cacheKey, content, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(60));
                    }

                    return content;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An unexpected error occurred while using the {SocialMediaPlatform} content provider.", provider.Platform);
                    return new(RequestContentResult.Error);
                }
            }
        }

        return _platformNotSupportedResult;
    }
}
