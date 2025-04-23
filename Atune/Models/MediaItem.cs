namespace Atune.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class MediaItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    // Связь с альбомом (один альбом – много треков)
    [ForeignKey("Album")]
    public int AlbumId { get; set; }
    public Album Album { get; set; } = null!;
    
    [Range(1900u, 2100u, ErrorMessage = "Invalid year")]
    public uint Year { get; set; }
    
    [Range(0, 10, ErrorMessage = "Rating must be between 0 and 10")]
    public double Rating { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime ReleaseDate { get; set; }
    
    public string CoverArt { get; set; } = string.Empty;
    
    public string Genre { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Path is required")]
    public string Path { get; set; } = string.Empty;
    
    public TimeSpan Duration { get; set; }
    
    // Связь с артистами (многие ко многим через TrackArtist)
    public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
    
    // Связь с плейлистами (многие ко многим через PlaylistMediaItem)
    public ICollection<PlaylistMediaItem> PlaylistMediaItems { get; set; } = new List<PlaylistMediaItem>();
    
    // Конструктор для удобства
    public MediaItem(string title, Album album, uint year, string genre, string path, TimeSpan duration, ICollection<TrackArtist> trackArtists)
    {
        Title = title;
        Album = album;
        AlbumId = album.Id;
        Year = year;
        Genre = genre;
        Path = path;
        Duration = duration;
        TrackArtists = trackArtists;
    }
    
    // Пустой конструктор для EF Core
    public MediaItem() { }
} 
