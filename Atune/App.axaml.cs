using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Atune.ViewModels;
using Atune.Views;
using System.Diagnostics.CodeAnalysis;
using Atune.Services;
using Atune.Models;
using ThemeVariant = Atune.Models.ThemeVariant;
using Avalonia.Platform;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace Atune;

public partial class App : Application
{
    public App()
    {
        try 
        {
            // Добавляем проверки на null
            if (Application.Current?.PlatformSettings != null)
            {
                Application.Current.PlatformSettings.ColorValuesChanged += OnSystemThemeChanged;
            }
        }
        catch 
        {
            // Игнорируем ошибки на платформах где нет PlatformSettings
        }
    }

    private void OnSystemThemeChanged(object? sender, PlatformColorValues e)
    {
        var settings = SettingsManager.LoadSettings();
        if (settings.ThemeVariant == ThemeVariant.System)
        {
            UpdateTheme(settings.ThemeVariant);
        }
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public new static App? Current => Application.Current as App;
    public IServiceProvider? Services { get; private set; }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();
        ServiceLocator.Initialize(serviceProvider);
        
        // Убираем дублирующийся код
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView
            {
                DataContext = serviceProvider.GetRequiredService<MainViewModel>()
            };
        }

        var settings = SettingsManager.LoadSettings();
        UpdateTheme(settings.ThemeVariant);

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<MediaViewModel>();
        services.AddTransient<HistoryViewModel>();
        
        services.AddTransient<MainView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<HomeView>();
        services.AddTransient<MediaView>();
        services.AddTransient<HistoryView>();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Disabled for Avalonia compatibility")]
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    public void UpdateTheme(ThemeVariant theme)
    {
        // Определяем актуальную тему
        var actualTheme = theme switch
        {
            ThemeVariant.System => Application.Current?.PlatformSettings?.GetColorValues()?.ThemeVariant 
                == PlatformThemeVariant.Dark 
                ? ThemeVariant.Dark 
                : ThemeVariant.Light,
            _ => theme
        };

        // Устанавливаем тему для всего приложения
        this.RequestedThemeVariant = actualTheme switch
        {
            ThemeVariant.Light => Avalonia.Styling.ThemeVariant.Light,
            ThemeVariant.Dark => Avalonia.Styling.ThemeVariant.Dark,
            _ => Avalonia.Styling.ThemeVariant.Default
        };

        // Принудительно обновляем все элементы интерфейса
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.InvalidateVisual();
        }
    }
}