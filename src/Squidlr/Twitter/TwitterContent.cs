
namespace Squidlr.Twitter;

public sealed class TwitterContent : Content
{
    public TweetIdentifier TweetIdentifier { get; }

    public int BookmarkCount { get; set; }

    public int QuoteCount { get; set; }

    public int RetweetCount { get; set; }

    public string? Source { get; set; }

    public int? Views { get; set; }

    public string? DisplayName { get; set; }

    public Uri? ProfileImageUri { get; set; }

    public TwitterContent(TweetIdentifier tweetIdentifier) : base(tweetIdentifier.Url, SocialMediaPlatform.Twitter)
    {
        TweetIdentifier = tweetIdentifier;
    }
}
