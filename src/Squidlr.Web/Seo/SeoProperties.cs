namespace Squidlr.Web.Seo;

public static class SeoProperties
{
    public const string AppName = "Squidlr";

    public const string Title = "Social Media Video Downloader - Squidlr";

    public const string Keywords = "x twitter instagram video downloader";

    public const string Description = "Download any video from X, Twitter and Instagram right now for free";

    public const string Domain = "squidlr.com";

    public static string CreatePageTitle(string? page = null)
    {
        if (!string.IsNullOrEmpty(page))
        {
            return $"{page} - {Title}";
        }

        return Title;
    }

    public static string DescriptionFor(string? platformName)
    {
        return $"Download any video from {platformName} right now for free";
    }

    public static string KeywordsFor(string? platformName)
    {
        return $"{platformName?.ToLowerInvariant()} video downloader";
    }
}
