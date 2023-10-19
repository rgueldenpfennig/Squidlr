namespace Squidlr.Abstractions;

public interface IUrlResolver
{
    SocialMediaPlatform ResolveUrl(string url);
}
