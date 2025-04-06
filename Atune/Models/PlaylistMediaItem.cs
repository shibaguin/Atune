using System;

namespace Atune.Models
{
    public class PlaylistMediaItem
    {
        public int PlaylistId { get; set; }
        public Playlist Playlist { get; set; } = null!;
        
        public int MediaItemId { get; set; }
        public MediaItem MediaItem { get; set; } = null!;
        
        // Поле для хранения позиции трека в плейлисте
        public int Position { get; set; }
        
        // Дата и время добавления трека в плейлист (по умолчанию CURRENT_TIMESTAMP)
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
} 