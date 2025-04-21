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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Avalonia.Input;
using Avalonia.Interactivity;

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
                var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
                // Toggle play/pause on Spacebar for desktop
                mainWindow.AddHandler(InputElement.KeyDownEvent,
                    new EventHandler<KeyEventArgs>((sender, args) =>
                    {
                        if (args.Key == Key.Space && mainWindow.DataContext is MainViewModel vm)
                            vm.TogglePlayPauseCommand.Execute(null);
                    }),
                    RoutingStrategies.Tunnel,
                    handledEventsToo: true);
                desktop.MainWindow = mainWindow;
                // Save playback state when window is closed
                mainWindow.Closing += (s, e) => SavePlaybackState();
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

            // Restore playback state is moved to MediaViewModel after loading media

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

    private void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        // Save playback state before shutdown
        Log.Information("Calling SavePlaybackState");
        SavePlaybackState();
        Log.Information("Application shutdown");
        Log.CloseAndFlush();
    }

    private void SavePlaybackState()
    {
        try
        {
            var platformPathService = Services?.GetService<IPlatformPathService>();
            var playbackService = Services?.GetService<MediaPlayerService>();
            if (platformPathService == null) return;

            // Use .txt format: each queue path on its own line, then index and position
            var filePath = platformPathService.GetSettingsPath("playbackstate.txt");
            Log.Information("Saving playback state to {Path}", filePath);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            if (desktop.MainWindow?.DataContext is not MainViewModel mainVm) return;
            var mediaVm = mainVm.MediaViewModelInstance;
            if (mediaVm == null) return;

            // Determine current queue index based on SelectedMediaItem
            int currentIndex = -1;
            if (mediaVm.SelectedMediaItem != null)
                currentIndex = mediaVm.PlaybackQueue.IndexOf(mediaVm.SelectedMediaItem);
            // Fallback to CurrentQueueIndex if SelectedMediaItem not set
            if (currentIndex < 0 && mediaVm.CurrentQueueIndex >= 0)
                currentIndex = mediaVm.CurrentQueueIndex;
            double position = playbackService?.Position.TotalSeconds ?? 0;

            using var writer = new StreamWriter(filePath, false);
            // Write queue paths
            foreach (var item in mediaVm.PlaybackQueue)
            {
                var path = item.Path?.Replace("\r", string.Empty).Replace("\n", string.Empty) ?? string.Empty;
                // Escape '|' if present
                writer.WriteLine(path.Replace("|", "\\|"));
            }
            // Marker lines: index and position
            writer.WriteLine($"__INDEX__:{currentIndex}");
            writer.WriteLine($"__POSITION__:{position.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

            Log.Information("Playback state saved, exists={Exists}", File.Exists(filePath));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save playback state");
        }
    }

    public async Task RestorePlaybackStateAsync()
    {
        var platformPathService = Services?.GetService<IPlatformPathService>();
        if (platformPathService == null) return;

        var filePath = platformPathService.GetSettingsPath("playbackstate.txt");
        Log.Information("Restoring playback state from {Path}", filePath);
        var directory = Path.GetDirectoryName(filePath);
        try
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                var files = Directory.GetFiles(directory);
                Log.Information("Restore directory '{Dir}' contains: {Files}", directory, files);
            }
        }
        catch (Exception dirEx)
        {
            Log.Error(dirEx, "Failed to list directory contents for {Dir}", directory);
        }

        if (!File.Exists(filePath))
        {
            Log.Warning("Playback state file not found: {Path}", filePath);
            return;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            var queuePaths = new List<string>();
            int stateIndex = -1;
            double statePos = 0;

            foreach (var raw in lines)
            {
                if (raw.StartsWith("__INDEX__:"))
                {
                    // Parse index after marker
                    int.TryParse(raw.Substring("__INDEX__:".Length), out stateIndex);
                }
                else if (raw.StartsWith("__POSITION__:"))
                {
                    // Parse position after marker
                    double.TryParse(raw.Substring("__POSITION__:".Length), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out statePos);
                }
                else
                {
                    // Unescape '|'
                    queuePaths.Add(raw.Replace("\\|", "|"));
                }
            }

            if (queuePaths.Count == 0) return;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow?.DataContext is MainViewModel mainVm)
            {
                mainVm.GoMediaCommand.Execute(null);
                var mediaVm = mainVm.MediaViewModelInstance;
                if (mediaVm == null) return;

                mediaVm.ClearQueueCommand.Execute(null);
                foreach (var path in queuePaths)
                {
                    var item = mediaVm.MediaItems.FirstOrDefault(mi => mi.Path == path);
                    if (item != null)
                        mediaVm.AddToQueueCommand.Execute(item);
                }

                if (stateIndex >= 0 && stateIndex < mediaVm.PlaybackQueue.Count)
                    mediaVm.SetQueuePositionCommand.Execute(stateIndex + 1);

                var playbackService = Services.GetService<MediaPlayerService>();
                if (playbackService != null && stateIndex >= 0 && stateIndex < mediaVm.PlaybackQueue.Count)
                {
                    var currentItem = mediaVm.PlaybackQueue[stateIndex];
                    // Stop any running media and load the saved track without playing
                    await playbackService.StopAsync();
                    await playbackService.Load(currentItem.Path);
                    // Seek to last known position on UI thread
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        playbackService.Position = TimeSpan.FromSeconds(statePos);
                    });

                    // Record the restored media path for resume logic
                    mainVm.CurrentMediaPath = currentItem.Path;
                    mainVm.CurrentMediaItem = currentItem;
                    mainVm.CurrentPosition = TimeSpan.FromSeconds(statePos);
                    mainVm.Duration = playbackService.Duration;
                    mainVm.IsPlaying = false;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in RestorePlaybackStateAsync");
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "DynamicallyAccessedMembers handled in registration")]
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>();
        services.AddDbContextFactory<AppDbContext>();
        // Register playlist repository and unit of work for playlist management
        services.AddScoped<IPlaylistRepository, PlaylistRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register service layer for playlists
        services.AddScoped<IPlaylistService, PlaylistService>();
        // Register utility service for admin/debug operations
        services.AddScoped<IUtilityService, UtilityService>();
        
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

        // Новая регистрация для MediaDatabaseService
        services.AddTransient<MediaDatabaseService>();
        // Register MediaFileService for file operations
        services.AddSingleton<MediaFileService>();
        
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
        services.AddTransient<MediaView>(sp => new MediaView(
            sp.GetRequiredService<MediaViewModel>(),
            sp.GetRequiredService<IDbContextFactory<AppDbContext>>(),
            sp.GetRequiredService<ILoggerService>(),
            sp.GetRequiredService<IMemoryCache>()
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

        services.AddScoped<IAlbumRepository, AlbumRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IPlaylistRepository, PlaylistRepository>();
        services.AddSingleton<IFoldersRepository, FoldersRepository>();

        services.AddSingleton<INavigationKeywordProvider, NavigationKeywordProvider>();

        // Регистрируем новый сервис воспроизведения
        services.AddSingleton<MediaPlayerService>();

        // Добавляем новый сервис для работы с обложками
        services.AddSingleton<ICoverArtService, CoverArtService>();
        services.AddSingleton<IPlayAlbumService, PlayAlbumService>();

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
    
    private async Task InitializeDatabaseAsync()
    {
        using var scope = Services!.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        try
        {
            Log.Information("Initializing database...");
            
            // Всегда применяем ожидающие миграции
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                Log.Information($"Applying {pendingMigrations.Count()} pending migrations...");
                await db.Database.MigrateAsync();
            }
            
            var exists = await db.MediaItems.AnyAsync();
            Log.Information($"Database status: {(exists ? "OK" : "EMPTY")}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database initialization failed");
            throw;
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