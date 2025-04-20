using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System.Collections.Generic;

namespace Atune.Converters
{
    public class CoverArtConverter : IValueConverter
    {
        // Static cache for loaded cover art bitmaps
        private static readonly Dictionary<string, Bitmap> _bitmapCache = new Dictionary<string, Bitmap>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            // Determine the key: use default cover if path invalid
            string key;
            if (string.IsNullOrEmpty(path) || (!path.StartsWith("avares://") && !File.Exists(path)))
            {
                key = "avares://Atune/Assets/default_cover.jpg";
            }
            else
            {
                key = path;
            }

            // Return cached bitmap if available
            if (_bitmapCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            // Load bitmap and cache it
            Bitmap bmp;
            try
            {
                bmp = new Bitmap(key);
            }
            catch
            {
                bmp = new Bitmap("avares://Atune/Assets/default_cover.jpg");
            }
            _bitmapCache[key] = bmp;
            return bmp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
} 