namespace Squidlr;

public sealed class UrlResolver
{
    private readonly IReadOnlyList<IUrlResolver> _urlResolvers;

    public UrlResolver(IReadOnlyList<IUrlResolver> urlResolvers)
    {
        ArgumentNullException.ThrowIfNull(urlResolvers);
        if (urlResolvers.Count == 0)
        {
            throw new ArgumentException($"No {nameof(IUrlResolver)} instance have been provided.", nameof(urlResolvers));
        }

        _urlResolvers = urlResolvers;
    }

    public SocialMediaPlatform ResolveUrl(string url)
    {
        for (var i = 0; i < _urlResolvers.Count; i++)
        {
            var resolvedPlatform = _urlResolvers[i].ResolveUrl(url);
            if (resolvedPlatform != SocialMediaPlatform.Unknown)
                return resolvedPlatform;
        }

        return SocialMediaPlatform.Unknown;
    }
}
