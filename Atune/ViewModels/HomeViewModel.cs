using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace Atune.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IMemoryCache _cache;
    
    public HomeViewModel(IMemoryCache cache)
    {
        _cache = cache;
        Title = "Главная страница";
        
        WelcomeMessage = _cache.GetOrCreate("WelcomeMessage", entry => 
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return GenerateWelcomeMessage() ?? string.Empty;
        })!; // Явное указание non-null
    }

    private string GenerateWelcomeMessage()
    {
        // Тяжёлые вычисления или загрузка данных
        return "Добро пожаловать в Atune!";
    }

    public string WelcomeMessage { get; } = string.Empty; // Инициализация по умолчанию
} 