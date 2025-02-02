using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using Atune.Models;

namespace Atune.ViewModels;

public partial class MediaViewModel : ViewModelBase
{
    private readonly IMemoryCache _cache;
    
    [ObservableProperty]
    private List<MediaItem> _mediaContent = new List<MediaItem>();

    public MediaViewModel(IMemoryCache cache)
    {
        _cache = cache;
        LoadMediaContent();
    }

    private void LoadMediaContent()
    {
        if (!_cache.TryGetValue("MediaContent", out List<MediaItem>? content))
        {
            content = LoadFromDataSource();
            _cache.Set("MediaContent", content, TimeSpan.FromMinutes(30));
        }
        MediaContent = content ?? new List<MediaItem>();
    }

    private List<MediaItem> LoadFromDataSource()
    {
        // Загрузка данных из внешнего источника
        return new List<MediaItem>();
    }
} 