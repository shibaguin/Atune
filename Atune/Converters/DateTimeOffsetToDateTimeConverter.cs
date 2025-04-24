using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia;

namespace Atune.Converters
{
    public class DateTimeOffsetToDateTimeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Convert VM DateTime to UI DateTimeOffset for DatePicker.SelectedDate
            if (value is DateTime dt)
                return new DateTimeOffset(dt);
            if (value is DateTimeOffset dto)
                return dto;
            // Return unset value if conversion fails
            return AvaloniaProperty.UnsetValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Convert UI DateTimeOffset back to VM DateTime
            if (value is DateTimeOffset dto)
                return dto.DateTime;
            // Return unset value if conversion fails
            return AvaloniaProperty.UnsetValue;
        }
    }
} 