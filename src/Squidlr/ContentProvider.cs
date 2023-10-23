using DotNext;
using Microsoft.Extensions.Caching.Memory;
using Squidlr.Abstractions;

namespace Squidlr;

public sealed class ContentProvider
{
    private readonly IReadOnlyList<IContentProvider> _contentProviders;

    private readonly IMemoryCache _memoryCache;

    private static readonly Result<Content, RequestContentResult> _platformNotSupportedResult = new (RequestContentResult.PlatformNotSupported);

    public ContentProvider(IReadOnlyList<IContentProvider> contentProviders, IMemoryCache memoryCache)
    {
        ArgumentNullException.ThrowIfNull(contentProviders);
        ArgumentNullException.ThrowIfNull(memoryCache);
        if (contentProviders.Count == 0)
        {
            throw new ArgumentException($"No {nameof(IContentProvider)} instances have been provided.", nameof(contentProviders));
        }

        _contentProviders = contentProviders;
        _memoryCache = memoryCache;
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
                var content = await provider.GetContentAsync(contentIdentifier.Url, cancellationToken);
                _memoryCache.Set(contentIdentifier.Url, content, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(15));

                return content;
            }
        }

        return _platformNotSupportedResult;
    }
}
