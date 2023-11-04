using Squidlr.Abstractions;
using Squidlr.Instagram.Utilities;

namespace Squidlr.Instagram;

public sealed class InstagramUrlResolver : IUrlResolver
{
    public ContentIdentifier ResolveUrl(string url)
    {
        if (UrlUtilities.IsValidInstagramUrl(url))
            return new ContentIdentifier(SocialMediaPlatform.Instagram, url);

        return ContentIdentifier.Unknown;
    }
}
