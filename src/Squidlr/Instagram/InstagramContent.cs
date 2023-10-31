using System.Collections.ObjectModel;

namespace Squidlr.Instagram;

public sealed class InstagramContent : Content
{
    public InstagramContent(string sourceUrl) : base(sourceUrl, SocialMediaPlatform.Instagram)
    {
    }

    public InstagramVideoCollection Videos { get; set; } = new();
}

public sealed class InstagramVideo
{
    public required Uri DisplayUrl { get; init; }

    public TimeSpan? Duration { get; set; }

    public VideoSourceCollection VideoSources { get; set; } = new();

    public InstagramVideo()
    {
    }
}

public sealed class InstagramVideoCollection : Collection<InstagramVideo>
{
}
