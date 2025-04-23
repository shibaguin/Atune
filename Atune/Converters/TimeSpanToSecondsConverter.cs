using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Atune.Converters;

public class TimeSpanToSecondsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
            return timeSpan.TotalSeconds;
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double seconds)
            return TimeSpan.FromSeconds(seconds);
        return TimeSpan.Zero;
    }
} 
