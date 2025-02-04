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

    public override void OnFrameworkInitializationCompleted()
    {
        try 
        {
            var services = new ServiceCollection();
            ConfigureServices(services); // Сначала конфигурируем сервисы
            var serviceProvider = services.BuildServiceProvider();
            Services = serviceProvider; // Затем присваиваем свойство Services

            // Оберните инициализацию БД в Task.Run
            Task.Run(() => {
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            });

            // Остальная инициализация
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
                desktop.Exit += OnAppExit;
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
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Ошибка инициализации приложения");
            throw;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "DynamicallyAccessedMembers handled in registration")]
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((provider, options) => 
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal), 
                "media_library.db");
            
            Console.WriteLine($"Database path: {dbPath}");
            options.UseSqlite($"Filename={dbPath}");
        });
        
        services.AddDbContextFactory<AppDbContext>(options => 
            options.UseSqlite("Data Source=media_library.db"));
        
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
                sp.GetRequiredService<AppDbContext>()
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
    
    private void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        ConfigureCachePolicies();
        Log.CloseAndFlush();
    }
}