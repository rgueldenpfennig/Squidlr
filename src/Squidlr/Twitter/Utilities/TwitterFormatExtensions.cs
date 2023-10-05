using System.Globalization;

namespace Squidlr.Twitter.Utilities;

internal static class TwitterFormatExtensions
{
    public static DateTimeOffset ParseToDateTimeOffset(this string value)
    {
        return DateTimeOffset.ParseExact(value, "ddd MMM dd HH:mm:ss zzz yyyy", CultureInfo.InvariantCulture);
    }
}
