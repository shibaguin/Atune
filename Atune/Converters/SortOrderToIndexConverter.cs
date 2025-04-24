namespace Atune.Converters
{
    using System;
    using System.Globalization;
    using Avalonia.Data.Converters;
    using Avalonia;

    public class SortOrderToIndexConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string s) return 0;
            var rd = Application.Current?.Resources;
            string a = rd?["Sort_AZ"]?.ToString() ?? "A-Z";
            string z = rd?["Sort_ZA"]?.ToString() ?? "Z-A";
            string old = rd?["Sort_OldFirst"]?.ToString() ?? "Сначала старые";
            string nw = rd?["Sort_NewFirst"]?.ToString() ?? "Сначала новые";

            if (s == a) return 0;
            if (s == z) return 1;
            if (s == old) return 2;
            if (s == nw) return 3;
            return 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            int idx = value is int i ? i : 0;
            var rd = Application.Current?.Resources;
            return idx switch
            {
                0 => rd?["Sort_AZ"]?.ToString() ?? "A-Z",
                1 => rd?["Sort_ZA"]?.ToString() ?? "Z-A",
                2 => rd?["Sort_OldFirst"]?.ToString() ?? "Сначала старые",
                3 => rd?["Sort_NewFirst"]?.ToString() ?? "Сначала новые",
                _ => rd?["Sort_AZ"]?.ToString() ?? "A-Z",
            };
        }
    }
}
