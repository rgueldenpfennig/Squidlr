namespace Squidlr;

public interface IUrlResolver
{
    SocialMediaPlatform ResolveUrl(string url);
}
