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

    public class CoverArtService(
        IPlaybackEngineService playbackEngineService,
        ILogger<CoverArtService> logger) : ICoverArtService
    {
        private readonly IPlaybackEngineService _playbackEngineService = playbackEngineService;
        private readonly ILogger<CoverArtService> _logger = logger;

        public Stream? GetEmbeddedCoverArt()
        {
            var mediaAb = _playbackEngineService.GetCurrentMedia();
            var libVlcAb = _playbackEngineService.GetLibVlc();

            if (mediaAb == null || libVlcAb == null) return null;

            // Unwrap abstractions to native types
            if (libVlcAb is not LibVLCWrapper libVlcWrapper || mediaAb is not MediaWrapper mediaWrapper)
                return null;
            try
            {
                var nativeLib = libVlcWrapper.Native;
                using var mp = new MediaPlayer(nativeLib);
                mp.Media = mediaWrapper.InnerMedia;
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
                var media = _playbackEngineService.GetCurrentMedia();
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
