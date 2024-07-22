using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Squidlr.Tiktok.Utilities;

public static partial class UrlUtilities
{
    [GeneratedRegex(@"https?:\/\/(www\.)?tiktok\.com\/(\S+)\/video\/(?<id>\d+).*?", RegexOptions.IgnoreCase)]
    private static partial Regex TiktokUrlRegex();

    public static bool TryGetTiktokIdentifier(string url, [NotNullWhen(true)] out TiktokIdentifier? identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        identifier = null;

        var match = TiktokUrlRegex().Match(url);
        if (!match.Success)
            return false;

        identifier = new(match.Groups["id"].Value, match.Groups[0].Value);
        return true;
    }

    public static TiktokIdentifier GetTiktokIdentifier(string url)
    {
        if (!TryGetTiktokIdentifier(url, out var identifier))
            throw new ArgumentException("The value represents no valid Tiktok URL.", nameof(url));

        return identifier.Value;
    }
}
