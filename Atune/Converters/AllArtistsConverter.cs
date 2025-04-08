using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Atune.Models; // Убедитесь, что TrackArtist и Artist находятся в этом пространстве имен

namespace Atune.Converters
{
    public class AllArtistsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value ожидается как IEnumerable (например, ICollection<TrackArtist>)
            if (value is IEnumerable trackArtists)
            {
                var artistNames = new List<string>();
                foreach (var item in trackArtists)
                {
                    if (item is TrackArtist trackArtist && trackArtist.Artist != null && !string.IsNullOrEmpty(trackArtist.Artist.Name))
                    {
                        artistNames.Add(trackArtist.Artist.Name);
                    }
                }
                return string.Join(", ", artistNames);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 