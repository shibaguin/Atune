using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace Atune.Models
{
    public class ArtistInfo
    {
        public string CoverArt { get; set; }
        public string ArtistName { get; set; }
        public int TrackCount { get; set; }
        public int AlbumCount { get; set; }
        public List<MediaItem> Tracks { get; set; }

        // Total duration of all artist tracks
        public TimeSpan TotalDuration => Tracks.Aggregate(TimeSpan.Zero, (sum, t) => sum + t.Duration);

        // Formatted duration string, include days if >=24h
        public string FormattedDuration
        {
            get
            {
                var dur = TotalDuration;
                if (dur.Days > 0)
                    return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", dur.Days, dur.Hours, dur.Minutes, dur.Seconds);
                return string.Format("{0:00}:{1:00}:{2:00}", dur.Hours, dur.Minutes, dur.Seconds);
            }
        }

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
