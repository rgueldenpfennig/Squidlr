﻿using Squidlr.Abstractions;

namespace Squidlr;

public sealed class UrlResolver
{
    private readonly IReadOnlyList<IUrlResolver> _urlResolvers;

    public UrlResolver(IReadOnlyList<IUrlResolver> urlResolvers)
    {
        ArgumentNullException.ThrowIfNull(urlResolvers);
        if (urlResolvers.Count == 0)
        {
            throw new ArgumentException($"No {nameof(IUrlResolver)} instances have been provided.", nameof(urlResolvers));
        }

        _urlResolvers = urlResolvers;
    }

    public ContentIdentifier ResolveUrl(string? url)
    {
        if (url == null)
            return ContentIdentifier.Unknown;

        for (var i = 0; i < _urlResolvers.Count; i++)
        {
            var contentIdentifier = _urlResolvers[i].ResolveUrl(url);
            if (contentIdentifier != ContentIdentifier.Unknown)
                return contentIdentifier;
        }

        return ContentIdentifier.Unknown;
    }

    public bool IsValidUrl(string? url)
    {
        return ResolveUrl(url) != ContentIdentifier.Unknown;
    }
}
