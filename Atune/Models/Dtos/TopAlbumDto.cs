namespace Atune.Models.Dtos
{
    public class TopAlbumDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CoverArtPath { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public uint Year { get; set; }
        public int TrackCount { get; set; }
        public int PlayCount { get; set; }
    }
} 