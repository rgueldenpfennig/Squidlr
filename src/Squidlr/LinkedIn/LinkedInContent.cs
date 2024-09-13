namespace Squidlr.LinkedIn;

public sealed class LinkedInContent : Content
{
    public LinkedInContent(string sourceUrl) : base(sourceUrl, SocialMediaPlatform.LinkedId)
    {
    }

    public override string ToString()
    {
        return SourceUrl;
    }
}
