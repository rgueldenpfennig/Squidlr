using System.Globalization;
using Squidlr.Shared;

namespace Squidlr.App.Converters;

public sealed class IntToMetricConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return null;
        if (targetType != typeof(string)) throw new NotImplementedException();
        if (value is not int intValue) throw new NotImplementedException();

        return FormatHelper.FormatNumber(intValue);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
