using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using Atune.Models;
using System;

namespace Atune.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly IMemoryCache _cache;
    
    [ObservableProperty]
    private List<HistoryItem> _historyItems = new List<HistoryItem>();

    public HistoryViewModel(IMemoryCache cache)
    {
        _cache = cache;
        LoadHistory();
    }

    private void LoadHistory()
    {
        HistoryItems = _cache.GetOrCreate("HistoryItems", entry => 
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            entry.Size = 1024;
            return FetchHistoryFromStorage();
        }) ?? new List<HistoryItem>();
    }

    private List<HistoryItem> FetchHistoryFromStorage()
    {
        // Loading history from storage
        // Загрузка истории из хранилища
        return new List<HistoryItem>();
    }
} 