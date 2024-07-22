namespace Squidlr.Web.Seo;

public static class SeoProperties
{
    public const string AppName = "Squidlr";

    public const string Title = "Social Media Video Downloader | Squidlr";

    public const string Keywords = "x twitter instagram tiktok video downloader";

    public const string Description = "Download any video from X, Twitter, Instagram and TikTok right now for free";

    public const string Domain = "squidlr.com";

    public static string CreatePageTitle(string? page = null)
    {
        if (!string.IsNullOrEmpty(page))
        {
            return $"{page} | {Title}";
        }

        return Title;
    }

    public static string PlatformPageTitleFor(string platformName)
    {
        ArgumentException.ThrowIfNullOrEmpty(platformName);
        return $"Download videos from {platformName} | {AppName}";
    }

    public static string DescriptionFor(string platformName)
    {
        ArgumentException.ThrowIfNullOrEmpty(platformName);
        return $"Download any video from {platformName} right now for free";
    }

    public static string KeywordsFor(string platformName)
    {
        ArgumentException.ThrowIfNullOrEmpty(platformName);
        return $"{platformName?.ToLowerInvariant()} video downloader";
    }
}
