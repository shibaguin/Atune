using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Atune.Converters;

public class DurationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            // If days are present, format as dd:hh:mm:ss
            if (timeSpan.Days > 0)
            {
                return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            }
            // Otherwise format as mm:ss
            return string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Not implemented
        return null;
    }
} 