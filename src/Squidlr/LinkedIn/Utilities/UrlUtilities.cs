using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Squidlr.LinkedIn.Utilities;

public static partial class UrlUtilities
{
    [GeneratedRegex(@"^https?:\/\/(www\.)?linkedin\.com\/posts\/.*-(?<id>[0-9]+)-\w*", RegexOptions.IgnoreCase)]
    private static partial Regex LinkedInUrlRegex();

    public static bool TryGetLinkedInIdentifier(string url, [NotNullWhen(true)] out LinkedInIdentifier? identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        identifier = null;

        var match = LinkedInUrlRegex().Match(url);
        if (!match.Success)
            return false;

        identifier = new(match.Groups["id"].Value, match.Groups[0].Value);
        return true;
    }

    public static LinkedInIdentifier GetLinkedInIdentifier(string url)
    {
        if (!TryGetLinkedInIdentifier(url, out var identifier))
            throw new ArgumentException("The value represents no valid LinkedIn URL.", nameof(url));

        return identifier.Value;
    }
}
