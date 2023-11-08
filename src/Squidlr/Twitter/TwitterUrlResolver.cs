using Squidlr.Abstractions;
using Squidlr.Twitter.Utilities;

namespace Squidlr.Twitter;

public sealed class TwitterUrlResolver : IUrlResolver
{
    public ContentIdentifier ResolveUrl(string url)
    {
        if (UrlUtilities.IsValidTwitterStatusUrl(url))
        {
            var tweetIdentifier = UrlUtilities.CreateTweetIdentifierFromUrl(url);
            return new ContentIdentifier(SocialMediaPlatform.Twitter, tweetIdentifier.Id, tweetIdentifier.Url);
        }

        return ContentIdentifier.Unknown;
    }
}
