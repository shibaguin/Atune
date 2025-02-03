using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System;
using Atune.Models;
using Avalonia;
using TagLib;
using Avalonia.Markup.Xaml;
using Atune.ViewModels;

namespace Atune.Views;

public partial class MediaView : UserControl
{
    public MediaView()
    {
        InitializeComponent();
    }
    
    public MediaView(MediaViewModel vm) : this()
    {
        DataContext = vm;
    }

    private async void AddToLibrary_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider == null) return;

            var files = await storageProvider.OpenFilePickerAsync(new()
            {
                Title = "Select audio files",
                AllowMultiple = true
            });

            if (files.Count > 0)
            {
                using var db = new AppDbContext();
                await db.Database.EnsureCreatedAsync();

                foreach (var file in files)
                {
                    try
                    {
                        var filePath = file.Path.AbsolutePath;
                        var tagFile = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(filePath));
                        var mediaItem = new MediaItem(
                            tagFile.Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(filePath),
                            tagFile.Tag.FirstPerformer ?? "Unknown Artist",
                            filePath,
                            tagFile.Properties.Duration
                        );

                        await db.MediaItems.AddAsync(mediaItem);
                    }
                    catch (Exception tagEx)
                    {
                        Console.WriteLine($"Ошибка чтения файла {file.Path}: {tagEx.Message}");
                        continue;
                    }
                }
                
                await db.SaveChangesAsync();
                // Показать уведомление об успешном добавлении
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}