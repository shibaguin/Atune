using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Atune.Models
{
    public class Album
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        public int Year { get; set; } // Год выпуска альбома

        public string CoverArtPath { get; set; } = string.Empty; // Путь к обложке альбома

        public int TrackCount => Tracks.Count; // Количество треков

        // Связь с артистами через связывающую таблицу
        public ICollection<AlbumArtist> AlbumArtists { get; set; } = new List<AlbumArtist>();

        // Один альбом имеет много треков
        public ICollection<MediaItem> Tracks { get; set; } = new List<MediaItem>();
    }
} 