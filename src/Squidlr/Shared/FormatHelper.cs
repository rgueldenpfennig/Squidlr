using Humanizer;

namespace Squidlr.Shared;

public static class FormatHelper
{
    public static string? FormatContentTitle(string? title)
    {
        if (string.IsNullOrEmpty(title)) return null;

        return title.Truncate(20, Truncator.FixedNumberOfWords).Replace(Environment.NewLine, "");
    }

    public static string FormatNumber(int value)
    {
        if (value >= 10_000)
            return value.ToMetric(decimals: 1).ToUpperInvariant();

        return "{0:N0}".FormatWith(value);
    }

    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds <= 60)
            return duration.Humanize();

        return duration.Humanize(precision: 2);
    }

    public static string ToQuantity(int quantity, string word)
    {
        return word.ToQuantity(quantity, ShowQuantityAs.None);
    }

    public static string CreateVideoResolutionText(VideoSource[] videos, int videoSourceIndex)
    {
        return videoSourceIndex switch
        {
            0 => "Best resolution",
            1 when videos.Length == 5 => "High resolution",
            2 when videos.Length == 5 => "Medium resolution",
            3 when videos.Length == 5 => "Low resolution",
            1 when videos.Length == 4 => "High resolution",
            2 when videos.Length == 4 => "Medium resolution",
            1 when videos.Length == 3 => "Medium resolution",
            _ => "Lowest resolution"
        };
    }
}
