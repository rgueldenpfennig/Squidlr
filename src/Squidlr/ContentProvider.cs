using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNext;
using Squidlr.Abstractions;

namespace Squidlr;

public sealed class ContentProvider
{
    private readonly IReadOnlyList<IContentProvider> _contentProviders;

    public ContentProvider(IReadOnlyList<IContentProvider> contentProviders)
    {
        ArgumentNullException.ThrowIfNull(contentProviders);
        if (contentProviders.Count == 0)
        {
            throw new ArgumentException($"No {nameof(IContentProvider)} instances have been provided.", nameof(contentProviders));
        }

        _contentProviders = contentProviders;
    }

    public ValueTask<Result<Content, RequestContentResult>> GetContentAsync(string url, SocialMediaPlatform platform, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        for (var i = 0; i < _urlResolvers.Count; i++)
        {
            var resolvedPlatform = _urlResolvers[i].ResolveUrl(url);
            if (resolvedPlatform != SocialMediaPlatform.Unknown)
                return resolvedPlatform;
        }

        return SocialMediaPlatform.Unknown;
    }
}
