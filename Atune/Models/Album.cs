using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Atune.Models
{
    public class Album
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        // Связь с артистами через связывающую таблицу
        public ICollection<AlbumArtist> AlbumArtists { get; set; } = new List<AlbumArtist>();

        // Один альбом имеет много треков
        public ICollection<MediaItem> Tracks { get; set; } = new List<MediaItem>();
    }
} 