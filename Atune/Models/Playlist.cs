using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Atune.Models
{
    public class Playlist
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        public bool IsSmart { get; set; } // Умный плейлист

        // Связь с треками через связывающую таблицу
        public IList<PlaylistMediaItem> PlaylistMediaItems { get; set; } = new List<PlaylistMediaItem>();
    }
} 