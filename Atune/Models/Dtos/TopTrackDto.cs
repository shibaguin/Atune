namespace Atune.Models.Dtos
{
    using System;

    public class TopTrackDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CoverArtPath { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public int PlayCount { get; set; }
        public int TrackCount { get; set; }
    }
} 