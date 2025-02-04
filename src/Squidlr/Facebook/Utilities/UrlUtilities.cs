using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Squidlr.Facebook.Utilities;

public static partial class UrlUtilities
{
    [GeneratedRegex(@"^https?:\/\/[www\.|m\.]+facebook\.com\/((\S+\/videos|reel\/|groups\/\d+\/posts\/)|((video.php|watch\/){1}\?v=)){1}(\S*\/)?(?<id>\d+).*?", RegexOptions.IgnoreCase)]
    private static partial Regex FacebookUrlRegex();

    [GeneratedRegex(@"^https?:\/\/[www\.|m\.]+facebook\.com\/share\/\w{1}\/(?<id>[\s\S][^\?\/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex FacebookShareUrlRegex();

    [GeneratedRegex(@"^https?:\/\/fb\.watch\/(?<id>[\s\S][^\?\/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex FacebookWatchUrlRegex();

    [GeneratedRegex(@"^https?:\/\/[www\.|m\.]+facebook\.com\/story.php\?story_fbid=[\d]+&id=(?<id>[\d]+){1}", RegexOptions.IgnoreCase)]
    private static partial Regex FacebookStoryUrlRegex();

    public static bool TryGetFacebookIdentifier(string url, [NotNullWhen(true)] out FacebookIdentifier? identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        identifier = null;

        var match = GetMatch(url);
        if (match is null)
            return false;

        identifier = new(match.Groups["id"].Value, match.Groups[0].Value);
        return true;
    }

    public static FacebookIdentifier GetFacebookIdentifier(string url)
    {
        if (!TryGetFacebookIdentifier(url, out var identifier))
            throw new ArgumentException("The value represents no valid Facebook URL.", nameof(url));

        return identifier.Value;
    }

    private static readonly Regex[] _expressions = [FacebookUrlRegex(), FacebookShareUrlRegex(), FacebookWatchUrlRegex(), FacebookStoryUrlRegex()];

    private static Match? GetMatch(string url)
    {
        foreach (var regex in _expressions)
        {
            var match = regex.Match(url);
            if (match.Success)
                return match;
        }

        return null;
    }
}
