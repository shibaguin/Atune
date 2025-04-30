using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.File;
using Serilog.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Atune.Data;
using Atune.Data.Interfaces;
using Atune.Data.Repositories;
using Atune.Models;
using Atune.Services;
using Atune.ViewModels;
using Atune.Views;
using Atune.Plugins;
using AutoMapper;
using Atune.Services.Interfaces;
using Atune.Extensions;

namespace Atune.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAtuneServices(this IServiceCollection services)
        {
            // Database contexts
            services.AddDbContext<AppDbContext>();
            services.AddDbContextFactory<AppDbContext>();

            // Repositories and UoW
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAlbumRepository, AlbumRepository>();
            services.AddScoped<IArtistRepository, ArtistRepository>();
            services.AddScoped<IPlaylistRepository, PlaylistRepository>();
            services.AddScoped<IPlayHistoryRepository, PlayHistoryRepository>();
            services.AddScoped<IMediaRepository>(provider =>
            {
                var context = provider.GetRequiredService<AppDbContext>();
                var baseRepo = new MediaRepository(context);
                return new CachedMediaRepository(baseRepo, provider.GetRequiredService<IMemoryCache>());
            });
            services.AddSingleton<IFoldersRepository, FoldersRepository>();
            services.AddScoped<IHomeRepository, HomeRepository>();

            // Unit of work with logger
            services.AddScoped<IUnitOfWork>(provider =>
                new UnitOfWork(provider.GetRequiredService<AppDbContext>(), provider.GetRequiredService<ILoggerService>()));

            // Domain services
            services.AddScoped<IPlaylistService, PlaylistService>();
            services.AddScoped<IUtilityService, UtilityService>();
            services.AddScoped<PlayHistoryService>();
            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<ISearchProvider, SectionSearchProvider>();
            services.AddScoped<ISearchProvider, SettingsSearchProvider>();
            services.AddScoped<ISearchProvider, AlbumSearchProvider>();
            services.AddScoped<ISearchProvider, TrackSearchProvider>();
            services.AddScoped<ISearchProvider, PlaylistSearchProvider>();
            services.AddScoped<ISearchProvider, ArtistSearchProvider>();

            // Caching and MemoryCache
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 100 * 1024 * 1024;
                options.CompactionPercentage = 0.25;
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
            });

            // Logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });

            // Platform and settings services
            services.AddSingleton<IPlatformPathService, PlatformPathService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IInterfaceSettingsService, InterfaceSettingsService>();
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddSingleton<LocalizationService>();

            // File and media services
            services.AddTransient<MediaDatabaseService>();
            services.AddSingleton<MediaFileService>();
            // VLC abstraction factory registrations for MediaPlayerService
            services.AddSingleton<ILibVLCFactory, LibVLCFactory>();
            services.AddSingleton<IMediaFactory, MediaFactory>();
            services.AddSingleton<IMediaPlayerFactory, MediaPlayerFactory>();
            services.AddSingleton<IDispatcherService, AvaloniaDispatcherService>();
            // Playback engine and high-level playback services
            services.AddSingleton<IPlaybackEngineService, MediaPlayerService>();
            // Also register the concrete service so that classes depending directly on MediaPlayerService can be injected
            services.AddSingleton<MediaPlayerService>(sp => (MediaPlayerService)sp.GetRequiredService<IPlaybackEngineService>());
            services.AddSingleton<IPlaybackService, PlaybackService>();
            services.AddSingleton<ICoverArtService, CoverArtService>();
            services.AddSingleton<IPlayAlbumService, PlayAlbumService>();
            services.AddSingleton<IPlayPlaylistService, PlayPlaylistService>();
            services.AddSingleton<IPlayArtistService, PlayArtistService>();

            // AutoMapper: регистрация профилей
            services.AddAutoMapper(typeof(MappingProfile).Assembly);

            // Сервисный слой: HomeService
            services.AddScoped<IHomeService, HomeService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<HomeViewModel>(sp => new HomeViewModel(
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<HomeViewModel>>(),
                sp.GetRequiredService<IHomeService>(),
                sp.GetRequiredService<IPlaybackService>(),
                sp.GetRequiredService<IMediaRepository>(),
                sp.GetRequiredService<IPlaylistRepository>(),
                sp.GetRequiredService<IAlbumRepository>()));
            services.AddTransient<MediaViewModel>();
            services.AddTransient<HistoryViewModel>(sp => new HistoryViewModel(
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<IUnitOfWork>(),
                sp.GetRequiredService<LocalizationService>()));
            services.AddTransient<SettingsViewModel>();
            services.AddScoped<SearchViewModel>();

            // Views
            services.AddTransient<MainWindow>();
            services.AddTransient<MainView>(sp => new MainView());
            services.AddTransient<HomeView>();
            services.AddTransient<MediaView>(sp => new MediaView(
                sp.GetRequiredService<MediaViewModel>(),
                sp.GetRequiredService<IDbContextFactory<AppDbContext>>(),
                sp.GetRequiredService<ILoggerService>(),
                sp.GetRequiredService<IMemoryCache>()));
            services.AddTransient<HistoryView>(sp => new HistoryView(sp.GetRequiredService<HistoryViewModel>()));
            services.AddTransient<SettingsView>(sp => new SettingsView(
                sp.GetRequiredService<SettingsViewModel>(),
                sp.GetRequiredService<ISettingsService>()));
            services.AddTransient<ArtistView>(sp => new ArtistView());
            services.AddTransient<ArtistListView>(sp => new ArtistListView());

            // ViewModel and Control factories
            services.AddSingleton<Func<Type, ViewModelBase>>(provider => type =>
                (ViewModelBase)provider.GetRequiredService(type));
            services.AddTransient<Func<Type, Control>>(provider => type =>
                (Control)ActivatorUtilities.CreateInstance(provider, type));

            // Plugins
            // Register PluginLoader as a hosted service to initialize plugins after host start
            services.AddHostedService<PluginLoader>();
            // Navigation keywords provider for MainViewModel
            services.AddSingleton<INavigationKeywordProvider, NavigationKeywordProvider>();

            return services;
        }
    }
}