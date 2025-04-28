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
            // Если есть дни, показываем dd:hh:mm:ss
            if (timeSpan.Days > 0)
            {
                return $"{timeSpan.Days:D2}:{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
            // Если есть часы, форматируем как h:mm:ss (без ведущего нуля в часах)
            if (timeSpan.Hours > 0)
            {
                return $"{timeSpan.Hours}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
            // Иначе mm:ss (без ведущего нуля в минутах)
            return $"{timeSpan.Minutes}:{timeSpan.Seconds:D2}";
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Not implemented
        return null;
    }
} 