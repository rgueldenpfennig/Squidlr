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
}
