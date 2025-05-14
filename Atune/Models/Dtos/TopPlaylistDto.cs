namespace Atune.Models.Dtos
{
    public class TopPlaylistDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CoverArtPath { get; set; } = string.Empty;
        public int TrackCount { get; set; }
        public int PlayCount { get; set; }
    }
}