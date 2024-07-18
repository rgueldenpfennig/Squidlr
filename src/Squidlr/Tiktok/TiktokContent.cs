namespace Squidlr.Tiktok;

public sealed class TiktokContent : Content
{
    public TiktokContent(string sourceUrl) : base(sourceUrl, SocialMediaPlatform.Tiktok)
    {
    }

    public int? PlayCount { get; set; }

    public int? CollectCount { get; set; }

    public int? ShareCount { get; set; }

    public override string ToString()
    {
        return SourceUrl;
    }
}

//public sealed class TiktokVideo
//{
//    public TimeSpan? Duration { get; set; }

//    public VideoSourceCollection VideoSources { get; set; } = new();

//    public TiktokVideo()
//    {
//    }
//}

//public sealed class TiktokVideoCollection : Collection<TiktokVideo>
//{
//}
