namespace Squidlr.Twitter;

public sealed class TwitterContent : Content
{
    public TweetIdentifier TweetIdentifier { get; }

    public int BookmarkCount { get; set; }

    public int QuoteCount { get; set; }

    public int RetweetCount { get; set; }

    public string? Source { get; set; }

    public int? Views { get; set; }

    public TweetMediaCollection Media { get; set; } = new TweetMediaCollection();

    public TwitterContent(TweetIdentifier tweetIdentifier) : base(tweetIdentifier.Url, SocialMediaPlatform.Twitter)
    {
        TweetIdentifier = tweetIdentifier;
    }

    public void AddMedia(TweetMedia media)
    {
        ArgumentNullException.ThrowIfNull(media);
        Media.Add(media);
    }
}
