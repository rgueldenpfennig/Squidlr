namespace Squidlr.Instagram;

public sealed class InstagramContent : Content
{
    public InstagramContent(string sourceUrl) : base(sourceUrl, SocialMediaPlatform.Instagram)
    {
    }

    public VideoSourceCollection VideoSources { get; set; } = new();
}
