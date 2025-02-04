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
    public int Year { get; set; }
    public string Genre { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Path is required")]
    public string Path { get; set; } = string.Empty;
    
    public TimeSpan Duration { get; set; }
    
    // Добавим конструктор для удобства
    public MediaItem(string title, string artist, string album, int year, string genre, string path, TimeSpan duration)
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
    public MediaItem() { }
} 