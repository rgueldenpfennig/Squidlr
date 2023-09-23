namespace Squidlr.Web.Seo;

public static class SeoProperties
{
    public const string Title = "X / Twitter Video Downloader - Squidlr";

    public const string Keywords = "x twitter video downloader";

    public const string Description = "Download any X / Twitter video simply right now";

    public const string Domain = "squidlr.com";

    public static string CreatePageTitle(string? page = null)
    {
        if (!string.IsNullOrEmpty(page))
        {
            return $"{page} - {Title}";
        }

        return Title;
    }
}
