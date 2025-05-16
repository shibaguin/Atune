using System;
using Avalonia.Data.Converters;
using System.Globalization;
using Avalonia;
using Avalonia.Layout;

namespace Atune.Converters
{
    public class BoolToHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isRightAligned)
            {
                return isRightAligned ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is HorizontalAlignment alignment)
            {
                return alignment == HorizontalAlignment.Right;
            }
            return false;
        }
    }
}