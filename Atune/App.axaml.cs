using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Atune.ViewModels;
using Atune.Views;
using System.Diagnostics.CodeAnalysis;
using Atune.Services;
using ThemeVariant = Atune.Models.ThemeVariant;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using System;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Avalonia.Controls;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Serilog.Sinks.File;
using Serilog.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Atune.Models;

namespace Atune;

public partial class App : Application
{
    public App()
    {
        try 
        {
            Dispatcher.UIThread.InvokeAsync(() => 
            {
                if (Application.Current?.PlatformSettings != null)
                {
                    Application.Current.PlatformSettings.ColorValuesChanged += OnSystemThemeChanged;
                }
            }, DispatcherPriority.Normal).Wait(TimeSpan.FromSeconds(1));
        }
        catch 
        {
            // Игнорируем таймауты
        }
    }

    ~App()
    {
        if (Application.Current?.PlatformSettings != null)
        {
            Application.Current.PlatformSettings.ColorValuesChanged -= OnSystemThemeChanged;
        }
    }

    private void OnSystemThemeChanged(object? sender, PlatformColorValues e)
    {
        var settingsService = Services?.GetRequiredService<ISettingsService>();
        if (settingsService == null) return;
        
        var settings = settingsService.LoadSettings();
        if (settings.ThemeVariant == ThemeVariant.System)
        {
            UpdateTheme(settings.ThemeVariant);
        }
    }

    public override void Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/atune-.log", 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        Log.Information("Инициализация приложения");
        
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    public new static App? Current => Application.Current as App;
    public IServiceProvider? Services { get; private set; }

    public override async void OnFrameworkInitializationCompleted()
    {
        try 
        {
            var services = new ServiceCollection();
            ConfigureServices(services); // Сначала конфигурируем сервисы
            var serviceProvider = services.BuildServiceProvider();
            Services = serviceProvider; // Затем присваиваем свойство Services

            // Применяем миграции и создаем БД
            await InitializeDatabaseAsync();

            // Остальная инициализация
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
                desktop.Exit += OnAppExit;
                desktop.Startup += (s, e) => 
                {
                    // Убрать удаление директории
                    // var dbDir = Path.Combine(AppContext.BaseDirectory, "Data");
                    // if (Directory.Exists(dbDir)) Directory.Delete(dbDir, true);
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                singleView.MainView = new MainView
                {
                    DataContext = serviceProvider.GetRequiredService<MainViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
            
            var settingsService = Services?.GetRequiredService<ISettingsService>();
            if (settingsService != null)
            {
                var settings = settingsService.LoadSettings();
                UpdateTheme(settings.ThemeVariant);
            }

            // Добавляем тестовую запись в БД
            var testItem = new MediaItem(
                "Test Title", 
                "Test Artist", 
                "Test Album", 
                2024, 
                "Test Genre",
                "/test/path.mp3", 
                TimeSpan.FromSeconds(123));
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal initialization error");
            File.WriteAllText("crash.log", $"CRITICAL ERROR: {ex}");
            Environment.Exit(1);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "DynamicallyAccessedMembers handled in registration")]
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>();
        services.AddDbContextFactory<AppDbContext>();
        
        // Остальные сервисы
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100 * 1024 * 1024; // 100 MB лимит
            options.CompactionPercentage = 0.25; // Освобождаем 25% места при достижении лимита
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // Частота проверки экспирации
        });
        services.AddSingleton<ViewLocator>();
        
        // Сервисы
        services.AddSingleton<ISettingsService, SettingsService>();
        
        // Явная регистрация ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<MediaViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Явная регистрация Views с конструкторами
        services.AddTransient<MainView>(sp => new MainView());
        services.AddTransient<HomeView>(sp => new HomeView(sp.GetRequiredService<HomeViewModel>()));
        services.AddTransient<MediaView>(sp => 
            new MediaView(
                sp.GetRequiredService<MediaViewModel>(),
                sp.GetRequiredService<IDbContextFactory<AppDbContext>>()
            ));
        services.AddTransient<HistoryView>(sp => new HistoryView());
        services.AddTransient<SettingsView>(sp => 
            new SettingsView(
                sp.GetRequiredService<SettingsViewModel>(),
                sp.GetRequiredService<ISettingsService>()
            )
        );
        services.AddTransient<MainWindow>();

        // Фабрики
        services.AddSingleton<Func<Type, ViewModelBase>>(provider => type => 
            (ViewModelBase)provider.GetRequiredService(type));

        services.AddTransient<Func<Type, Control>>(provider => 
            ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type) =>
                (Control)ActivatorUtilities.CreateInstance(provider, type));

        services.AddLogging(builder => {
            builder.AddSerilog(dispose: true);
        });
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
        // Добавляем отложенное выполнение для корректного определения системной темы
        Dispatcher.UIThread.Post(() =>
        {
            var actualTheme = theme switch
            {
                ThemeVariant.System => Application.Current?.PlatformSettings?.GetColorValues()?.ThemeVariant 
                    == PlatformThemeVariant.Dark 
                    ? ThemeVariant.Dark 
                    : ThemeVariant.Light,
                _ => theme
            };

            this.RequestedThemeVariant = actualTheme switch
            {
                ThemeVariant.Light => Avalonia.Styling.ThemeVariant.Light,
                ThemeVariant.Dark => Avalonia.Styling.ThemeVariant.Dark,
                _ => Avalonia.Styling.ThemeVariant.Default
            };

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow?.InvalidateVisual();
                desktop.MainWindow?.Activate();
            }
        }, DispatcherPriority.Background);
    }

    private void ConfigureCachePolicies()
    {
        if (Services?.GetService<IMemoryCache>() is MemoryCache memoryCache)
        {
            // Используем стандартный метод очистки кэша
            memoryCache.Compact(percentage: 0.25);
        }
    }
    
    private async void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try 
        {
            ConfigureCachePolicies();
            Log.CloseAndFlush();
            
            // Убираем проверку на IsApplicationExiting
            // Даем фиксированное время на завершение операций
            await Task.Delay(300).ConfigureAwait(false);
        }
        catch (TaskCanceledException) 
        {
            // Игнорируем отмену задач
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при завершении приложения");
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        using var scope = Services!.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        try
        {
            Console.WriteLine("Initializing database...");
            await db.Database.EnsureCreatedAsync();
            
            // Простая проверка работоспособности
            var exists = await db.MediaItems.AnyAsync();
            Console.WriteLine($"Database status: {(exists ? "OK" : "EMPTY")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DATABASE ERROR: {ex}");
            await RecreateDatabase(db);
        }
    }

    private async Task RecreateDatabase(AppDbContext db)
    {
        try
        {
            Console.WriteLine("Attempting database recreation...");
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            Console.WriteLine("Database recreated successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL RECREATION ERROR: {ex}");
            throw new InvalidOperationException("Cannot initialize database", ex);
        }
    }
}