using Squidlr.Abstractions;
using Squidlr.Instagram.Utilities;

namespace Squidlr.Instagram;

public sealed class InstagramUrlResolver : IUrlResolver
{
    public ContentIdentifier ResolveUrl(string url)
    {
        if (UrlUtilities.TryGetInstagramIdentifier(url, out var instagramIdentifier))
            return new ContentIdentifier(SocialMediaPlatform.Instagram, instagramIdentifier.Value.Url);

        return ContentIdentifier.Unknown;
    }
}
