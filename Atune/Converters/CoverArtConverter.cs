using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Atune.Converters
{
    public class CoverArtConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            // If no path or file doesn't exist (and not an embedded resource), use default cover
            if (string.IsNullOrEmpty(path) || (!path.StartsWith("avares://") && !File.Exists(path)))
            {
                return new Bitmap("avares://Atune/Assets/default_cover.jpg");
            }
            try
            {
                return new Bitmap(path);
            }
            catch
            {
                return new Bitmap("avares://Atune/Assets/default_cover.jpg");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
} 