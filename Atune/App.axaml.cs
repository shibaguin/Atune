using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using Atune.Converters;
using Atune.Extensions;
using Atune.Startup;
using ThemeVariant = Atune.Models.ThemeVariant;
using Atune.ViewModels;
using Atune.Views;
using Atune.Services;
using Atune.Services.Interfaces;

namespace Atune;

public partial class App : Application
{
    // --------------------------------------------------
    // Host and DI Setup
    // --------------------------------------------------
    private readonly IHost _host;

    public App()
    {
        // Build generic host with DI and logging
        _host = new HostBuilder()
            .UseSerilog((context, config) => config
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Atune", "logs", "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .MinimumLevel.Debug())
            .ConfigureServices((context, services) =>
                services.AddAtuneServices())
            .Build();
        // Start the host to initialize hosted services (including PluginLoader)
        _host.Start();

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
        // --------------------------------------------------
        // XAML Initialization and DI Provider Assignment
        // --------------------------------------------------
        Log.Information("Starting base initialization: loading XAML and setting up services");

        AvaloniaXamlLoader.Load(this);
        base.Initialize();

        // Use host's service provider for DI
        Services = _host.Services;
    }

    public new static App? Current => Application.Current as App;
    public IServiceProvider? Services { get; private set; }

    public override async void OnFrameworkInitializationCompleted()
    {
        // --------------------------------------------------
        // Application Initialization: Database and Window
        // --------------------------------------------------
        Log.Information("Full initialization completed: the main window has been created, the database and services are configured");

        try
        {
            // Initialize database
            await DatabaseInitializer.InitializeAsync(Services!);

            // Initialize application windows
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Disable Avalonia DataAnnotations validation plugin
                AvaloniaValidationDisabler.Disable();
                var mainWindow = Services!.GetRequiredService<MainWindow>();
                var windowSettingsService = Services!.GetRequiredService<WindowSettingsService>();
                var settings = windowSettingsService.GetCurrentSettings();
                var isInitializing = true;

                Log.Information("Restoring window settings: {@Settings}", settings);

                // Применяем сохраненные настройки окна
                if (settings.IsMaximized)
                {
                    Log.Information("Restoring maximized window state");
                    mainWindow.WindowState = WindowState.Maximized;
                }
                else
                {
                    Log.Information("Restoring window position and size: X={X}, Y={Y}, Width={Width}, Height={Height}",
                        settings.X, settings.Y, settings.Width, settings.Height);
                    mainWindow.Position = new PixelPoint((int)settings.X, (int)settings.Y);
                    mainWindow.Width = settings.Width;
                    mainWindow.Height = settings.Height;
                }

                // Устанавливаем текущую страницу
                if (mainWindow.DataContext is MainViewModel mainVm)
                {
                    if (Enum.TryParse<MainViewModel.SectionType>(settings.CurrentPage, out var section))
                    {
                        Log.Information("Restoring current page: {Page}", settings.CurrentPage);
                        mainVm.SelectedSection = section;
                    }
                    else
                    {
                        Log.Warning("Failed to parse current page: {Page}", settings.CurrentPage);
                    }
                }

                // Сохраняем изменения положения окна
                mainWindow.PositionChanged += (s, e) =>
                {
                    if (!isInitializing && mainWindow.WindowState != WindowState.Maximized)
                    {
                        var (validatedX, validatedY) = windowSettingsService.ValidateWindowPosition(
                            mainWindow.Position.X,
                            mainWindow.Position.Y,
                            mainWindow.Width,
                            mainWindow.Height
                        );

                        // Применяем валидированную позицию
                        if (validatedX != mainWindow.Position.X || validatedY != mainWindow.Position.Y)
                        {
                            mainWindow.Position = new PixelPoint((int)validatedX, (int)validatedY);
                        }

                        settings.X = validatedX;
                        settings.Y = validatedY;
                        windowSettingsService.SaveSettingsAsync(settings).ConfigureAwait(false);
                    }
                };

                // Сохраняем изменения размера окна
                mainWindow.SizeChanged += (s, e) =>
                {
                    if (!isInitializing && mainWindow.WindowState != WindowState.Maximized)
                    {
                        var (validatedWidth, validatedHeight) = windowSettingsService.ValidateWindowSize(
                            mainWindow.Width,
                            mainWindow.Height
                        );

                        // Применяем валидированные размеры
                        if (validatedWidth != mainWindow.Width || validatedHeight != mainWindow.Height)
                        {
                            mainWindow.Width = validatedWidth;
                            mainWindow.Height = validatedHeight;
                        }

                        settings.Width = validatedWidth;
                        settings.Height = validatedHeight;
                        windowSettingsService.SaveSettingsAsync(settings).ConfigureAwait(false);
                    }
                };

                // Сохраняем состояние максимизации
                mainWindow.PropertyChanged += (s, e) =>
                {
                    if (!isInitializing && e.Property == Window.WindowStateProperty)
                    {
                        settings.IsMaximized = mainWindow.WindowState == WindowState.Maximized;
                        windowSettingsService.SaveSettingsAsync(settings).ConfigureAwait(false);
                    }
                };

                // Отключаем флаг инициализации после загрузки окна
                mainWindow.Loaded += (s, e) =>
                {
                    isInitializing = false;
                };

                // Сохраняем настройки при закрытии окна
                mainWindow.Closing += async (s, e) =>
                {
                    Log.Information("Window is closing, saving settings");
                    if (mainWindow.DataContext is MainViewModel mainVm)
                    {
                        settings.CurrentPage = mainVm.SelectedSection.ToString();
                        Log.Information("Current page before closing: {Page}", settings.CurrentPage);
                    }
                    if (mainWindow.WindowState != WindowState.Maximized)
                    {
                        settings.X = mainWindow.Position.X;
                        settings.Y = mainWindow.Position.Y;
                        settings.Width = mainWindow.Width;
                        settings.Height = mainWindow.Height;
                        Log.Information("Window position and size before closing: X={X}, Y={Y}, Width={Width}, Height={Height}",
                            settings.X, settings.Y, settings.Width, settings.Height);
                    }
                    settings.IsMaximized = mainWindow.WindowState == WindowState.Maximized;
                    Log.Information("Window maximized state before closing: {IsMaximized}", settings.IsMaximized);

                    // Сначала сохраняем настройки окна
                    await windowSettingsService.ForceSaveSettingsAsync();

                    // Даем время на сохранение файла
                    await Task.Delay(100);

                    // Затем сохраняем состояние воспроизведения
                    PlaybackStateManager.SaveState(Services!);
                };

                // Toggle play/pause on Spacebar for desktop
                mainWindow.AddHandler(InputElement.KeyDownEvent,
                    new EventHandler<KeyEventArgs>((sender, args) =>
                    {
                        if (args.Key == Key.Space && args.Source is not TextBox && mainWindow.DataContext is MainViewModel vm && args.KeyModifiers == KeyModifiers.None)
                        {
                            vm.TogglePlayPauseCommand.Execute(null);
                            args.Handled = true;
                        }
                    }),
                    RoutingStrategies.Tunnel,
                    handledEventsToo: true);
                desktop.MainWindow = mainWindow;
                // Handle saving playback state on window closing
                mainWindow.Closing += (s, e) => PlaybackStateManager.SaveState(Services!);
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
                    DataContext = Services!.GetRequiredService<MainViewModel>()
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
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error during full application initialization");
            File.WriteAllText("crash.log", $"CRITICAL ERROR: {ex}");
            Environment.Exit(1);
        }
    }

    // --------------------------------------------------
    // Playback State Management
    // --------------------------------------------------
    private void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        // Save playback state before shutdown
        Log.Information("Calling SavePlaybackState");
        PlaybackStateManager.SaveState(Services!);
        Log.Information("Application shutdown");
        Log.CloseAndFlush();
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

    // --------------------------------------------------
    // Localization Update
    // --------------------------------------------------
    public void UpdateLocalization()
    {
        // Добавляем принудительное обновление кэша
        var settingsService = Services?.GetRequiredService<ISettingsService>();
        // Сбрасываем кэш и загружаем заново
        settingsService?.LoadSettings();

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
