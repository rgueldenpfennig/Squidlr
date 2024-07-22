using Squidlr.Abstractions;
using Squidlr.Tiktok.Utilities;

namespace Squidlr.Tiktok;

public sealed class TiktokUrlResolver : IUrlResolver
{
    public ContentIdentifier ResolveUrl(string url)
    {
        if (UrlUtilities.TryGetTiktokIdentifier(url, out var TiktokIdentifier))
            return new ContentIdentifier(SocialMediaPlatform.Tiktok, TiktokIdentifier.Value.Id, TiktokIdentifier.Value.Url);

        return ContentIdentifier.Unknown;
    }
}
