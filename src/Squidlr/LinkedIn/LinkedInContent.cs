namespace Squidlr.LinkedIn;

public sealed class LinkedInContent : Content
{
    public string? UserUrl { get; set; }

    public LinkedInContent(string sourceUrl) : base(sourceUrl, SocialMediaPlatform.LinkedIn)
    {
    }

    public override string ToString()
    {
        return SourceUrl;
    }
}
