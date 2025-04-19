using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atune.Models
{
    public class PlaybackQueueItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int MediaItemId { get; set; }

        [ForeignKey(nameof(MediaItemId))]
        public MediaItem MediaItem { get; set; } = null!;

        // Position of the track in the playback queue
        public int Position { get; set; }

        // Date and time when the track was added to the queue
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
} 