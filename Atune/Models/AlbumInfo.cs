using System.Collections.Generic;

namespace Atune.Models
{
    public class AlbumInfo
    {
        public string AlbumName { get; set; }
        public string ArtistName { get; set; }
        public uint Year { get; set; }
        public int TrackCount { get; set; }
        public List<MediaItem> Tracks { get; set; }

        public AlbumInfo(string albumTitle, string artistName, uint year, List<MediaItem> tracks)
        {
            AlbumName = albumTitle;
            ArtistName = artistName;
            Year = year;
            Tracks = tracks;
            TrackCount = tracks.Count;
        }
    }
} 