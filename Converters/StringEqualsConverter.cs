using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ClaudeLauncher.Converters;

public class StringEqualsConverter : IValueConverter
{
    public static readonly StringEqualsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var a = (value as string) ?? "";
        var b = (parameter as string) ?? "";
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
