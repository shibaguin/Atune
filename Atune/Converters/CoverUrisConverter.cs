using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Atune.Models;

namespace Atune.Converters
{
    public class CoverUrisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var uris = new List<string>();
            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    switch (item)
                    {
                        case PlaylistMediaItem pmi:
                            if (!string.IsNullOrWhiteSpace(pmi.MediaItem.CoverArt))
                                uris.Add(pmi.MediaItem.CoverArt);
                            break;
                        case MediaItem mi:
                            if (!string.IsNullOrWhiteSpace(mi.CoverArt))
                                uris.Add(mi.CoverArt);
                            break;
                    }
                }
            }
            return uris.Distinct().Take(4).ToList();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
} 