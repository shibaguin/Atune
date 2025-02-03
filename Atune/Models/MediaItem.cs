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
    
    [Required(ErrorMessage = "Path is required")]
    public string Path { get; set; } = string.Empty;
    
    public TimeSpan Duration { get; set; }
    
    // Добавим конструктор для удобства
    public MediaItem(string title, string artist, string path, TimeSpan duration)
    {
        Title = title;
        Artist = artist;
        Path = path;
        Duration = duration;
    }
    
    // Пустой конструктор для EF Core
    public MediaItem() { }
} 