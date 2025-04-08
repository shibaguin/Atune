using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Atune.Models;
using System.Collections.Generic;

namespace Atune.Converters
{
    public class AllArtistsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is IEnumerable<TrackArtist> trackArtists)
            {
                var artistNames = trackArtists
                    .Select(ta => ta.Artist?.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name));
                return string.Join(", ", artistNames);
            }
            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 