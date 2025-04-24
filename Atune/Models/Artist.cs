using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Atune.Models
{
    public class Artist
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Дата создания
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Дата обновления

        // Связь с альбомами через связывающую таблицу
        public ICollection<AlbumArtist> AlbumArtists { get; set; } = new List<AlbumArtist>();

        // Связь с треками через связывающую таблицу
        public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
    }
}
