using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Atune.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<HomeViewModel> _logger;
    
    [ObservableProperty]
    private string _welcomeMessage = string.Empty;
    
    public HomeViewModel(
        IMemoryCache cache,
        ILogger<HomeViewModel> logger)
    {
        _cache = cache;
        _logger = logger;
        _logger.LogInformation("Initialization HomeViewModel");
        
        try {
            Title = "Main page";
            
            WelcomeMessage = _cache.GetOrCreate("WelcomeMessage", entry => 
            {
                entry.SetSize(1024)
                     .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                return GenerateWelcomeMessage() ?? string.Empty;
            })!;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error loading welcome message");
        }
    }

    private string GenerateWelcomeMessage()
    {
        // Heavy calculations or data loading
        // Тяжелые вычисления или загрузка данных
        return "Welcome to Atune!";
    }

    public async Task LoadWelcomeMessageAsync()
    {
        var message = await _cache.GetOrCreateAsync("WelcomeMessage", async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
            return await GenerateWelcomeMessageAsync();
        });
        WelcomeMessage = message ?? string.Empty;
    }

    private async Task<string> GenerateWelcomeMessageAsync()
    {
        await Task.CompletedTask;
        return "Welcome to Atune!";
    }
} 