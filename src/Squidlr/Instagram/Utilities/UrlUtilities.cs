using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Squidlr.Instagram.Utilities;

public static partial class UrlUtilities
{
    [GeneratedRegex(@"^https?:\/\/[www\.]*instagram\.com\/[\S]*[p|tv|reel|stories]{1}[\/\S]?\/(?<id>[a-zA-Z0-9_-]+).*?", RegexOptions.IgnoreCase)]
    private static partial Regex InstagramUrlRegex();

    public static bool TryGetInstagramIdentifier(string url, [NotNullWhen(true)] out InstagramIdentifier? identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        identifier = null;

        var match = InstagramUrlRegex().Match(url);
        if (!match.Success)
            return false;

        identifier = new(match.Groups["id"].Value, match.Groups[0].Value);
        return true;
    }

    public static InstagramIdentifier GetInstagramIdentifier(string url)
    {
        if (!TryGetInstagramIdentifier(url, out var identifier))
            throw new ArgumentException("The value represents no valid Instagram URL.", nameof(url));

        return identifier.Value;
    }
}
