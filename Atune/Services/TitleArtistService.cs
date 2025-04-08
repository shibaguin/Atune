using System;
using System.IO;
using System.Linq;
using LibVLCSharp.Shared;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Atune.Services
{
    public static class TitleArtistService
    {
        private static readonly Dictionary<string, string> _titleCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string? Sanitize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) 
                return null;
            
            return input.Trim()
                .Replace("\0", "")
                .Replace("\uFFFD", "")
                .Replace("%20", " ")
                .Replace("_", " ")
                .Normalize();
        }

        public static string ParseArtist(Media media, string path)
        {
            try
            {
                var rawArtist = media.Meta(MetadataType.Artist);
                var rawAlbumArtist = media.Meta(MetadataType.AlbumArtist);
                
                Debug.WriteLine($"Raw artist data: Artist={rawArtist}, AlbumArtist={rawAlbumArtist}");
                
                var artist = Sanitize(rawArtist) ?? Sanitize(rawAlbumArtist) ?? string.Empty;
                
                if (string.IsNullOrWhiteSpace(artist))
                {
                    artist = GetFallbackArtist(path);
                    Debug.WriteLine($"Using fallback artist: {artist}");
                }

                return artist;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Artist parse error: {ex}");
                return GetFallbackArtist(path);
            }
        }

        public static string ParseTitle(Media media, string path)
        {
            try
            {
                var title = Sanitize(media.Meta(MetadataType.Title));
                
                if (string.IsNullOrEmpty(title))
                {
                    var fileName = Uri.UnescapeDataString(path);
                    title = Path.GetFileNameWithoutExtension(fileName);
                    title = Sanitize(title) ?? "Неизвестный трек";
                }
                
                return title;
            }
            catch
            {
                return GetFallbackTitle(path);
            }
        }

        public static string GetFallbackArtist(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "Неизвестный исполнитель";

            string cacheKey = "artist_" + path;
            if (_titleCache.TryGetValue(cacheKey, out string? cachedArtist))
                return cachedArtist!;

            var dirName = Path.GetDirectoryName(Uri.UnescapeDataString(path))?
                      .Split(Path.DirectorySeparatorChar)
                      .LastOrDefault();
            string artist = Sanitize(dirName) ?? "Неизвестный исполнитель";
            _titleCache[cacheKey] = artist;
            return artist;
        }

        public static string GetFallbackTitle(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "Неизвестный трек";
        
            if (_titleCache.TryGetValue(path, out string? cachedTitle))
                return cachedTitle!;
        
            var fileName = Path.GetFileNameWithoutExtension(Uri.UnescapeDataString(path));
            string title = Sanitize(fileName) ?? "Неизвестный трек";
            _titleCache[path] = title;
            return title;
        }
    }
} 