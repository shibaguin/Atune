using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace Atune.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IMemoryCache _cache;
    
    [ObservableProperty]
    private string _welcomeMessage = string.Empty;
    
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

    public async Task LoadWelcomeMessageAsync()
    {
        var message = await _cache.GetOrCreateAsync("WelcomeMessage", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await GenerateWelcomeMessageAsync();
        });
        WelcomeMessage = message ?? string.Empty;
    }

    private async Task<string> GenerateWelcomeMessageAsync()
    {
        await Task.CompletedTask;
        return "Добро пожаловать в Atune!";
    }
} 