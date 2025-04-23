using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Atune;
using Atune.Services;

namespace Atune.Converters
{
    public class CoverArtConverter : IValueConverter
    {
        // URI for the built-in default cover image (embedded Avalonia resource)
        public const string DefaultCoverUri = "avares://Atune/Assets/default_cover.jpg";
        // Cache for loaded bitmaps
        private static readonly Dictionary<string, Bitmap> _bitmapCache = new Dictionary<string, Bitmap>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var input = value as string;
            // Get the path to the default cover file in user data
            var pathService = App.Current.Services.GetRequiredService<IPlatformPathService>();
            var defaultCover = pathService.GetDefaultCoverPath();
            // Decide which path to load: input file if it exists, else the default file path if it exists, else embedded resource URI
            string key;
            if (!string.IsNullOrWhiteSpace(input) && File.Exists(input))
                key = input!;
            else if (!string.IsNullOrWhiteSpace(defaultCover) && File.Exists(defaultCover))
                key = defaultCover;
            else
                key = DefaultCoverUri;

            // Return cached bitmap if available
            if (_bitmapCache.TryGetValue(key, out var cachedBmp))
                return cachedBmp;

            // Load the bitmap from the chosen key
            Bitmap bmp;
            try
            {
                bmp = new Bitmap(key);
            }
            catch
            {
                // Fallback to embedded default resource
                bmp = new Bitmap(DefaultCoverUri);
                key = DefaultCoverUri;
            }

            // Cache and return
            _bitmapCache[key] = bmp;
            return bmp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
} 
