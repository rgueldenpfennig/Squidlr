using Squidlr.Abstractions;
using Squidlr.Twitter.Utilities;

namespace Squidlr.Twitter;

public sealed class TwitterUrlResolver : IUrlResolver
{
    public SocialMediaPlatform ResolveUrl(string url)
    {
        if (UrlUtilities.IsValidTwitterStatusUrl(url))
            return SocialMediaPlatform.Twitter;

        return SocialMediaPlatform.Unknown;
    }
}
