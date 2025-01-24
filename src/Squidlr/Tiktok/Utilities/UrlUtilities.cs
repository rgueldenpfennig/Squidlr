using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Squidlr.Tiktok.Utilities;

public static partial class UrlUtilities
{
    [GeneratedRegex(@"https?:\/\/(www\.)?tiktok\.com\/(\S+)\/video\/(?<id>\d+).*?", RegexOptions.IgnoreCase)]
    private static partial Regex TiktokUrlRegex();

    [GeneratedRegex(@"https?:\/\/\w+.tiktok\.com\/(\w{1}\/)?(?<id>[^@][\S][^\/\?]+){1}.*?", RegexOptions.IgnoreCase)]
    private static partial Regex TiktokShareUrlRegex();

    public static bool TryGetTiktokIdentifier(string url, [NotNullWhen(true)] out TiktokIdentifier? identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        identifier = null;

        var isShareUrl = false;
        var match = TiktokUrlRegex().Match(url);
        if (!match.Success)
        {
            match = TiktokShareUrlRegex().Match(url);
            if (!match.Success)
                return false;
            isShareUrl = true;
        }

        identifier = new(match.Groups["id"].Value, match.Groups[0].Value, isShareUrl);
        return true;
    }

    public static TiktokIdentifier GetTiktokIdentifier(string url)
    {
        if (!TryGetTiktokIdentifier(url, out var identifier))
            throw new ArgumentException("The value represents no valid Tiktok URL.", nameof(url));

        return identifier.Value;
    }
}
