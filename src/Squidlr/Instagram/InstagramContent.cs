namespace Squidlr.Instagram;

public sealed class InstagramContent : Content
{
    public InstagramContent(string sourceUrl) : base(sourceUrl, SocialMediaPlatform.Instagram)
    {
    }

    public string? FullName { get; set; }

    public Uri? ProfilePictureUrl { get; set; }

    public override string ToString()
    {
        return SourceUrl;
    }
}
