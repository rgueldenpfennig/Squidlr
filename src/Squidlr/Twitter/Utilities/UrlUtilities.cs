using System.Text.RegularExpressions;
using Squidlr.Twitter;

namespace Squidlr.Utilities;

public static partial class UrlUtilities
{
    [GeneratedRegex(@"^https?:\/\/(www\.)?(mobile\.)?(twitter|x)\.com\/\w+\/status\/(?<statusId>\d+).*?", RegexOptions.IgnoreCase)]
    private static partial Regex TwitterStatusUrlRegex();

    public static bool IsValidTwitterStatusUrl(string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        return TwitterStatusUrlRegex().IsMatch(url);
    }

    [GeneratedRegex(@"^https?:\/\/video\.twimg\.com\/.+\.(mp4).*?", RegexOptions.IgnoreCase)]
    private static partial Regex TwitterVideoUrlRegex();

    public static bool IsValidTwitterVideoUrl(string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        return TwitterVideoUrlRegex().IsMatch(url);
    }

    public static VideoSize ParseSizeFromVideoUrl(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        var indexEnd = url.LastIndexOf('/');
        var indexBegin = url.LastIndexOf('/', indexEnd - 1);
        var range = url[(indexBegin + 1)..indexEnd].AsSpan();

        var xIndex = range.IndexOf('x');
        if (xIndex == -1)
        {
            return VideoSize.Empty;
        }

        return new()
        {
            Height = int.Parse(range[(xIndex + 1)..]),
            Width = int.Parse(range[..xIndex]),
        };
    }

    public static TweetIdentifier CreateTweetIdentifierFromUrl(string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        var match = TwitterStatusUrlRegex().Match(url);
        if (!match.Success)
            throw new ArgumentException("The value represents no valid Twitter status URL.", nameof(url));

        var statusUrl = match.Groups[0].Value;
        var statusId = match.Groups["statusId"].Value;

        return new(statusId, statusUrl);
    }
}