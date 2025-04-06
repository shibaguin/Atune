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
using Microsoft.Extensions.Hosting;
using Atune.Data.Interfaces;
using Atune.Data.Repositories;
using Atune.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Atune;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = new HostBuilder()
            .UseSerilog((context, config) => config
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atune", "logs", "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .MinimumLevel.Debug())
            .Build();

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
            // Ignore timeouts
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
        // Log the start of base initialization: loading XAML and setting up DI services
        Log.Information("Starting base initialization: loading XAML and setting up services");
        
        AvaloniaXamlLoader.Load(this);
        base.Initialize();

        var services = new ServiceCollection();
        // Register the service for platform-specific paths
        services.AddSingleton<IPlatformPathService, PlatformPathService>();
        // Register MemoryCache for SettingsService
        services.AddMemoryCache();
        // Register the logger implementation (ensure LoggerService implements ILoggerService)
        services.AddSingleton<ILoggerService, LoggerService>();
        // Register SettingsService, which requires IMemoryCache, IPlatformPathService and ILoggerService
        services.AddSingleton<ISettingsService, SettingsService>();
        // Register LocalizationService
        services.AddSingleton<LocalizationService>();

        // Build the dependency provider
        Services = services.BuildServiceProvider();

        // Get the service through the registered provider
        var localizationService = Services.GetRequiredService<LocalizationService>();
    }

    public new static App? Current => Application.Current as App;
    public IServiceProvider? Services { get; private set; }

    public override async void OnFrameworkInitializationCompleted()
    {
        // Log the completion of full initialization: the main window has been created, the database and services are configured
        Log.Information("Full initialization completed: the main window has been created, the database and services are configured");
        
        try 
        {
            var services = new ServiceCollection();
            ConfigureServices(services); // сначала конфигурируем сервисы
            var serviceProvider = services.BuildServiceProvider();
            Services = serviceProvider; // затем присваиваем свойство Services

            // Apply migrations and create the database
            await InitializeDatabaseAsync();

            // Initialize application windows
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
                desktop.Exit += OnAppExit;
                desktop.Startup += (s, e) =>
                {
                    // Additional actions when the application starts (if necessary)
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

            // Example of adding a test entry to the database
            var testItem = new MediaItem(
                "Title", 
                new Album { Title = "Album" }, 
                2023u, 
                "Genre", 
                "/path/to/file.mp3", 
                TimeSpan.FromMinutes(3), 
                new List<TrackArtist>()
            );
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error during full application initialization");
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
        // Other services
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100 * 1024 * 1024; // 100 MB limit
            options.CompactionPercentage = 0.25; // Free up 25% space when the limit is reached
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // Frequency of checking expiration
        });
        services.AddSingleton<ViewLocator>();
        
        // Register the platform-specific service and settings service
        // Регистрация сервиса для платформенно-специфичных путей и сервиса настроек
        services.AddSingleton<IPlatformPathService, PlatformPathService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IInterfaceSettingsService, InterfaceSettingsService>();
        services.AddSingleton<ILoggerService, LoggerService>();
        services.AddSingleton<LocalizationService>();

        // Явное регистрация ViewModels
        // Explicit registration of ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<MediaViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Явное регистрация View с конструкторами
        // Explicit registration of Views with constructors
        services.AddTransient<MainView>(sp => new MainView());
        services.AddTransient<HomeView>(sp => new HomeView(sp.GetRequiredService<HomeViewModel>()));
        services.AddTransient<MediaView>(sp => 
            new MediaView(
                sp.GetRequiredService<MediaViewModel>(),
                sp.GetRequiredService<IDbContextFactory<AppDbContext>>(),
                sp.GetRequiredService<ILoggerService>()
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
        // Factories
        services.AddSingleton<Func<Type, ViewModelBase>>(provider => type => 
            (ViewModelBase)provider.GetRequiredService(type));

        services.AddTransient<Func<Type, Control>>(provider => 
            ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type) =>
                (Control)ActivatorUtilities.CreateInstance(provider, type));

        services.AddLogging(builder => {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Добавляем новые сервисы
        // Add new services
        services.AddScoped<IUnitOfWork>(provider => 
            new UnitOfWork(
                provider.GetRequiredService<AppDbContext>(),
                provider.GetRequiredService<ILoggerService>()));
        
        services.AddScoped<IMediaRepository>(provider => 
        {
            var context = provider.GetRequiredService<AppDbContext>();
            var logger = provider.GetRequiredService<ILoggerService>();
            var baseRepo = new MediaRepository(context);
            return new CachedMediaRepository(
                baseRepo, 
                provider.GetRequiredService<IMemoryCache>()
            );
        });

        services.AddSingleton<INavigationKeywordProvider, NavigationKeywordProvider>();

        // Регистрируем новый сервис воспроизведения
        services.AddSingleton<MediaPlayerService>();

        // Добавляем новый сервис для работы с обложками
        services.AddSingleton<ICoverArtService, CoverArtService>();

        // Добавляем загрузчик плагинов
        var pluginLoader = new PluginLoader(services.BuildServiceProvider().GetRequiredService<IPlatformPathService>(), services.BuildServiceProvider().GetRequiredService<ILoggerService>());
        var plugins = pluginLoader.LoadPlugins();

        foreach (var plugin in plugins)
        {
            plugin.Value.Initialize();
            services.AddSingleton(plugin.Value);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Disabled for Avalonia compatibility")]
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Получаем массив плагинов для удаления
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // Удаляем каждый найденный плагин
        // Remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    public void UpdateTheme(ThemeVariant theme)
    {
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

            // Принудительно обновляем все окна
            // Forcefully update all windows
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
            // Use the standard cache cleanup method
            memoryCache.Compact(percentage: 0.25);
        }
    }
    
    private void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        Log.Information("Application shutdown");
        Log.CloseAndFlush();
    }

    private async Task InitializeDatabaseAsync()
    {
        using var scope = Services!.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        try
        {
            Log.Information("Initializing database...");
            
            // Корректная проверка наличия таблицы MediaItems с использованием ExecuteScalarAsync
            bool tableExists = false;
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='MediaItems'";
                var result = await command.ExecuteScalarAsync();
                tableExists = Convert.ToInt32(result) > 0;
            }
            await connection.CloseAsync();

            if (!tableExists)
            {
                // Применение миграций, если таблицы еще нет
                var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    Log.Information($"Applying {pendingMigrations.Count()} pending migrations...");
                    await db.Database.MigrateAsync();
                }
            }
            
            var exists = await db.MediaItems.AnyAsync();
            Log.Information($"Database status: {(exists ? "OK" : "EMPTY")}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DATABASE ERROR");
            await RecreateDatabase(db);
        }
    }

    private async Task RecreateDatabase(AppDbContext db)
    {
        try
        {
            Log.Information("Attempting database recreation...");
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            Log.Information("Database recreated successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "FATAL RECREATION ERROR");
            throw new InvalidOperationException("Cannot initialize database", ex);
        }
    }

    public void UpdateLocalization()
    {
        // Добавляем принудительное обновление кэша
        var settingsService = Services?.GetRequiredService<ISettingsService>();
        if (settingsService != null)
        {
            // Сбрасываем кэш и загружаем заново
            settingsService.LoadSettings();
        }

        var localizationService = Services?.GetRequiredService<LocalizationService>();
        if (localizationService != null && settingsService != null)
        {
            var settings = settingsService.LoadSettings();
            localizationService.SetLanguage(settings.Language);
            
            // Принудительно обновляем все окна
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow?.InvalidateVisual();
            }
        }
    }
}