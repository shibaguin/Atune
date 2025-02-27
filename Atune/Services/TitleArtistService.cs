using System;
using System.IO;
using System.Linq;
using LibVLCSharp.Shared;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Atune.Services
{
    public static class TitleArtistService
    {
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
                
                var artist = Sanitize(rawArtist) ?? Sanitize(rawAlbumArtist);
                
                if (string.IsNullOrEmpty(artist))
                {
                    artist = GetFallbackArtist(path);
                    Debug.WriteLine($"Using fallback artist: {artist}");
                }

                return artist ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Artist parse error: {ex}");
                return GetFallbackArtist(path) ?? string.Empty;
            }
        }

        public static string ParseTitle(Media media, string path)
        {
            try
            {
                var title = Sanitize(media.Meta(MetadataType.Title));
                
                if (string.IsNullOrEmpty(title))
                {
                    var fileName = Uri.UnescapeDataString(path); // Декодируем URL
                    title = Path.GetFileNameWithoutExtension(fileName);
                    title = Sanitize(title) ?? "Неизвестный трек";
                }
                
                return title ?? string.Empty;
            }
            catch
            {
                return GetFallbackTitle(path) ?? string.Empty;
            }
        }

        public static string GetFallbackArtist(string path)
        {
            var dirName = Path.GetDirectoryName(Uri.UnescapeDataString(path))?
                .Split(Path.DirectorySeparatorChar)
                .LastOrDefault();
            
            return Sanitize(dirName) ?? "Неизвестный исполнитель";
        }

        public static string GetFallbackTitle(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(Uri.UnescapeDataString(path));
            return Sanitize(fileName) ?? "Неизвестный трек";
        }
    }
} 