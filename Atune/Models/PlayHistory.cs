namespace Atune.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PlayHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey("MediaItem")]
    public int MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; } = null!;

    public DateTime PlayedAt { get; set; }
    public int DurationSeconds { get; set; }
    public Guid SessionId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public double PercentPlayed { get; set; }
    public string AppVersion { get; set; } = string.Empty;
    public string OS { get; set; } = string.Empty;
}