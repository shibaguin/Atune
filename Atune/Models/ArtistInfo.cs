using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Atune.Models
{
    public class ArtistInfo
    {
        public string CoverArt { get; set; }
        public string ArtistName { get; set; }
        public int TrackCount { get; set; }
        public int AlbumCount { get; set; }
        public List<MediaItem> Tracks { get; set; }

        public ArtistInfo(string artistName, List<MediaItem> tracks)
        {
            ArtistName = artistName;
            Tracks = tracks;
            TrackCount = tracks.Count;
            AlbumCount = tracks
                .Select(t => t.Album?.Title)
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Distinct()
                .Count();
            var firstTrack = Tracks.FirstOrDefault();
            string? artPath = firstTrack?.CoverArt;
            if (string.IsNullOrEmpty(artPath) || (!artPath.StartsWith("avares://") && !File.Exists(artPath)))
            {
                artPath = "avares://Atune/Assets/default_cover.jpg";
            }
            CoverArt = artPath!;
        }
    }
} 