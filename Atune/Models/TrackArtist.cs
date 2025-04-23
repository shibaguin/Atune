namespace Atune.Models
{
    public class TrackArtist
    {
        public int MediaItemId { get; set; }
        public MediaItem MediaItem { get; set; } = null!;
        
        public int ArtistId { get; set; }
        public Artist Artist { get; set; } = null!;
    }
} 
