namespace Squidlr.Facebook;

public sealed class FacebookContent : Content
{
    public int ShareCount { get; set; }

    public FacebookContent(string sourceUrl) : base(sourceUrl, SocialMediaPlatform.Facebook)
    {
    }

    public override string ToString()
    {
        return SourceUrl;
    }
}
