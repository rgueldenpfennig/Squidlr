using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Squidlr.Facebook.Utilities;

public static partial class UrlUtilities
{
    [GeneratedRegex(@"^https?:\/\/(www\.|m\.)?facebook\.com\/((\S+\/videos|reel\/|groups\/\d+\/posts\/)|((video.php|watch\/){1}\?v=)){1}(\S*\/)?(?<id>\d+).*?", RegexOptions.IgnoreCase)]
    private static partial Regex FacebookUrlRegex();

    [GeneratedRegex(@"^https?:\/\/(www\.|m\.)?facebook\.com\/share\/\w{1}\/(?<id>[\s\S][^\?\/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex FacebookShareUrlRegex();

    [GeneratedRegex(@"^https?:\/\/fb\.watch\/(?<id>[\s\S][^\?\/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex FacebookWatchUrlRegex();

    public static bool TryGetFacebookIdentifier(string url, [NotNullWhen(true)] out FacebookIdentifier? identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        identifier = null;

        var match = GetMatch(url, out var isShareUrl);
        if (!match.Success)
            return false;

        identifier = new(match.Groups["id"].Value, match.Groups[0].Value, isShareUrl);
        return true;
    }

    public static FacebookIdentifier GetFacebookIdentifier(string url)
    {
        if (!TryGetFacebookIdentifier(url, out var identifier))
            throw new ArgumentException("The value represents no valid Facebook URL.", nameof(url));

        return identifier.Value;
    }

    private static Match GetMatch(string url, out bool isMatch)
    {
        isMatch = false;

        var match = FacebookUrlRegex().Match(url);
        if (!match.Success)
        {
            match = FacebookShareUrlRegex().Match(url);
            if (!match.Success)
                match = FacebookWatchUrlRegex().Match(url);
        }

        return match;
    }
}
