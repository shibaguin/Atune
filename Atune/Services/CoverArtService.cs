using System;
using System.IO;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using Avalonia.Media.Imaging;
using System.Threading.Tasks;

namespace Atune.Services
{
    public interface ICoverArtService
    {
        Stream? GetEmbeddedCoverArt();
        string? GetCoverArtPath();
        Bitmap? LoadCoverFromPath(string? path);
    }

    public class CoverArtService : ICoverArtService
    {
        private readonly MediaPlayerService _mediaPlayerService;
        private readonly ILogger<CoverArtService> _logger;

        public CoverArtService(
            MediaPlayerService mediaPlayerService,
            ILogger<CoverArtService> logger)
        {
            _mediaPlayerService = mediaPlayerService;
            _logger = logger;
        }

        public Stream? GetEmbeddedCoverArt()
        {
            var media = _mediaPlayerService.GetCurrentMedia();
            var libVlc = _mediaPlayerService.GetLibVlc();
            
            if (media == null || libVlc == null) return null;
            
            try 
            {
                using var mp = new MediaPlayer(libVlc);
                mp.Media = media;
                mp.Play();
                
                // Добавляем задержку для инициализации плеера
                Task.Delay(100).Wait();
                
                var tempFile = Path.GetTempFileName() + ".png";
                if (mp.TakeSnapshot(0u, tempFile, 200u, 200u))
                {
                    var imageBytes = File.ReadAllBytes(tempFile);
                    File.Delete(tempFile);
                    return new MemoryStream(imageBytes);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting embedded cover art");
                return null;
            }
        }

        public string? GetCoverArtPath()
        {
            try
            {
                var media = _mediaPlayerService.GetCurrentMedia();
                if (media == null) return null;
                
                media.Parse(MediaParseOptions.ParseLocal | MediaParseOptions.FetchLocal);
                
                var artUrl = media.Meta(MetadataType.ArtworkURL) 
                           ?? media.Meta(MetadataType.Publisher);
                
                if (string.IsNullOrEmpty(artUrl)) return null;

                if (Uri.TryCreate(artUrl, UriKind.Absolute, out var uri))
                {
                    return uri.IsFile && File.Exists(uri.LocalPath) 
                        ? uri.LocalPath 
                        : null;
                }
                return File.Exists(artUrl) ? artUrl : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error accessing cover art path");
                return null;
            }
        }

        public Bitmap? LoadCoverFromPath(string? path)
        {
            try
            {
                return !string.IsNullOrEmpty(path) && File.Exists(path) 
                    ? new Bitmap(path) 
                    : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cover from path: {Path}", path);
                return null;
            }
        }
    }
} 
