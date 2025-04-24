using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

namespace Atune.Models
{
    public class AlbumInfo
    {
        public string CoverArt { get; set; }
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }
        public uint Year { get; set; }
        public int TrackCount { get; set; }
        // Total duration of all tracks in the album
        public TimeSpan TotalDuration => Tracks.Aggregate(TimeSpan.Zero, (sum, t) => sum + t.Duration);
        // Formatted duration string, include days if >=24h
        public string FormattedDuration
        {
            get
            {
                var dur = TotalDuration;
                if (dur.Days > 0)
                    return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", dur.Days, dur.Hours, dur.Minutes, dur.Seconds);
                else
                    return string.Format("{0:00}:{1:00}:{2:00}", dur.Hours, dur.Minutes, dur.Seconds);
            }
        }
        public List<MediaItem> Tracks { get; set; }

        public AlbumInfo(string albumTitle, string artistName, uint year, List<MediaItem> tracks)
        {
            AlbumName = albumTitle;
            ArtistName = artistName;
            Year = year;
            Tracks = tracks;
            TrackCount = tracks.Count;
            // Determine album cover: prefer track's cover, then album entity path, else default
            var firstTrack = Tracks.FirstOrDefault();
            string? artPath = firstTrack?.CoverArt;
            // If track cover missing or file doesn't exist, try album's CoverArtPath
            if (string.IsNullOrEmpty(artPath) || (!artPath.StartsWith("avares://") && !File.Exists(artPath)))
                artPath = firstTrack?.Album?.CoverArtPath;
            // If still missing or file not found, use default resource cover
            if (string.IsNullOrEmpty(artPath) || (!artPath.StartsWith("avares://") && !File.Exists(artPath)))
                artPath = "avares://Atune/Assets/default_cover.jpg";
            CoverArt = artPath!;
        }
    }
}
