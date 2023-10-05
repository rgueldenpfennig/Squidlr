namespace Squidlr.Twitter;

public sealed class TweetMediaVideo : TweetMedia
{
    public TimeSpan? Duration { get; set; }

    public VideoSourceCollection VideoSources { get; set; } = new();

    public TweetMediaVideo() : base(TweetMediaType.Video)
    {
    }
}
