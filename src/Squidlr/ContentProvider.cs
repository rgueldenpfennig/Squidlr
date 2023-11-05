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
        if (_memoryCache.TryGetValue<Result<Content, RequestContentResult>>(contentIdentifier.Url, out var result))
            return result;

        for (var i = 0; i < _contentProviders.Count; i++)
        {
            var provider = _contentProviders[i];
            if (provider.Platform == contentIdentifier.Platform)
            {
                try
                {
                    var content = await provider.GetContentAsync(contentIdentifier.Url, cancellationToken);
                    if (content.Error == RequestContentResult.Success || content.Error == RequestContentResult.NotFound)
                    {
                        _memoryCache.Set(contentIdentifier.Url, content, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(60));
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
