using Squidlr.Abstractions;
using Squidlr.LinkedIn.Utilities;

namespace Squidlr.LinkedIn;

public sealed class LinkedInUrlResolver : IUrlResolver
{
    public ContentIdentifier ResolveUrl(string url)
    {
        if (UrlUtilities.TryGetLinkedInIdentifier(url, out var linkedInIdentifier))
            return new ContentIdentifier(SocialMediaPlatform.LinkedIn, linkedInIdentifier.Value.Id, linkedInIdentifier.Value.Url);

        return ContentIdentifier.Unknown;
    }
}
