using System.Globalization;

namespace Squidlr.Utilities;

internal static class TwitterFormatExtensions
{
    public static DateTimeOffset ParseToDateTimeOffset(this string value)
    {
        return DateTimeOffset.ParseExact(value, "ddd MMM dd HH:mm:ss zzz yyyy", CultureInfo.InvariantCulture);
    }
}
