namespace Atune.Models.Dtos
{
    using System;

    public class RecentTrackDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CoverArtPath { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public DateTime LastPlayedAt { get; set; }
    }
}