namespace Atune.Models;
using System;
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
    
    public string Artist { get; set; } = string.Empty;
    
    public string Album { get; set; } = string.Empty;
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
    
    // Добавляем конструктор для удобства
    // Add a constructor for convenience
    public MediaItem(string title, string artist, string album, uint year, string genre, string path, TimeSpan duration)
    {
        Title = title;
        Artist = artist;
        Album = album;
        Year = year;
        Genre = genre;
        Path = path;
        Duration = duration;
    }
    
    // Пустой конструктор для EF Core
    // Empty constructor for EF Core
    public MediaItem() { }
} 