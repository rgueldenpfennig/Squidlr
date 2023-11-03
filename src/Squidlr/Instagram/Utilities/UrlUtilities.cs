using System.Text.RegularExpressions;

namespace Squidlr.Instagram.Utilities;

public static partial class UrlUtilities
{
    [GeneratedRegex(@"^https?:\/\/(www\.)?instagram\.com\/(\S+)?(p|tv|reel)\/(?<id>[a-zA-Z0-9_-]+).*?", RegexOptions.IgnoreCase)]
    private static partial Regex InstagramUrlRegex();

    public static bool IsValidInstagramUrl(string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        return InstagramUrlRegex().IsMatch(url);
    }

    public static InstagramIdentifier GetInstagramIdentifier(string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        var match = InstagramUrlRegex().Match(url);
        if (!match.Success)
            throw new ArgumentException("The value represents no valid Instagram URL.", nameof(url));

        return new(match.Groups["id"].Value, match.Groups[0].Value);
    }
}
