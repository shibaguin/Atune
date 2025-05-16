using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Atune.Data.Interfaces;
using Atune.Models.Dtos;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using System.Linq;
using Atune.Services;
using System.Collections.Generic;
using Atune.Views;
using Atune.ViewModels;
using Avalonia.Controls;
using Atune.Models;
using Atune.Services.Interfaces;

namespace Atune.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<HomeViewModel> _logger;
    private readonly IHomeService _homeService;
    private readonly IPlaybackService _playbackService;
    private readonly IMediaRepository _mediaRepository;
    private readonly IPlaylistRepository _playlistRepository;
    private readonly IAlbumRepository _albumRepository;

    [ObservableProperty]
    private string _welcomeMessage = string.Empty;

    public ObservableCollection<TopTrackDto> TopTracks { get; } = new();
    public ObservableCollection<TopAlbumDto> TopAlbums { get; } = new();
    public ObservableCollection<TopPlaylistDto> TopPlaylists { get; } = new();
    public ObservableCollection<RecentTrackDto> RecentTracks { get; } = new();

    public HomeViewModel(
        IMemoryCache cache,
        ILogger<HomeViewModel> logger,
        IHomeService homeService,
        IPlaybackService playbackService,
        IMediaRepository mediaRepository,
        IPlaylistRepository playlistRepository,
        IAlbumRepository albumRepository)
    {
        _cache = cache;
        _logger = logger;
        _homeService = homeService;
        _playbackService = playbackService;
        _mediaRepository = mediaRepository;
        _playlistRepository = playlistRepository;
        _albumRepository = albumRepository;
        _logger.LogInformation("Initialization HomeViewModel");

        try
        {
            Title = "Main page";

            WelcomeMessage = _cache.GetOrCreate("WelcomeMessage", entry =>
            {
                entry.SetSize(1024)
                     .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                return GenerateWelcomeMessage() ?? string.Empty;
            })!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading welcome message");
        }

        // Load home data (top lists and recent)
        _ = LoadDataAsync();
    }

    private static string GenerateWelcomeMessage()
    {
        // Heavy calculations or data loading
        // Тяжелые вычисления или загрузка данных
        return "Welcome to Atune!";
    }

    public async Task LoadWelcomeMessageAsync()
    {
        var message = await _cache.GetOrCreateAsync("WelcomeMessage", async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromHours(1))
                 .SetSize(1024);
            return await GenerateWelcomeMessageAsync();
        });
        WelcomeMessage = message ?? string.Empty;
    }

    private static async Task<string> GenerateWelcomeMessageAsync()
    {
        await Task.CompletedTask;
        return "Welcome to Atune!";
    }

    public async Task LoadDataAsync()
    {
        try
        {
            _logger.LogInformation("HomeViewModel.LoadDataAsync called");
            var tracks = await _homeService.GetTopTracksAsync() ?? Enumerable.Empty<TopTrackDto>();
            _logger.LogInformation("HomeViewModel: Loaded TopTracks count = {Count}", tracks.Count());
            Dispatcher.UIThread.Post(() =>
            {
                TopTracks.Clear();
                foreach (var t in tracks) TopTracks.Add(t);
            });

            var albums = (await _homeService.GetTopAlbumsAsync()) ?? Enumerable.Empty<TopAlbumDto>();
            _logger.LogInformation("HomeViewModel: Loaded TopAlbums count = {Count}", albums.Count());
            // Логируем детали полученных DTO для отладки дублирования
            foreach (var a in albums)
            {
                _logger.LogInformation("HomeViewModel: TopAlbum DTO - Id:{Id}, Title:'{Title}', Tracks:{TrackCount}, Plays:{PlayCount}", a.Id, a.Title, a.TrackCount, a.PlayCount);
            }
            Dispatcher.UIThread.Post(() =>
            {
                TopAlbums.Clear();
                foreach (var a in albums)
                    TopAlbums.Add(a);
            });

            var playlists = await _homeService.GetTopPlaylistsAsync() ?? Enumerable.Empty<TopPlaylistDto>();
            _logger.LogInformation("HomeViewModel: Loaded TopPlaylists count = {Count}", playlists.Count());
            Dispatcher.UIThread.Post(() =>
            {
                TopPlaylists.Clear();
                foreach (var p in playlists) TopPlaylists.Add(p);
            });

            var recents = await _homeService.GetRecentTracksAsync() ?? Enumerable.Empty<RecentTrackDto>();
            _logger.LogInformation("HomeViewModel: Loaded RecentTracks count = {Count}", recents.Count());
            Dispatcher.UIThread.Post(() =>
            {
                RecentTracks.Clear();
                foreach (var r in recents) RecentTracks.Add(r);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in LoadDataAsync");
        }
    }

    [RelayCommand]
    private async Task PlayTopTrack(TopTrackDto dto)
    {
        _playbackService.ClearQueue();
        var trackDtos = (await _homeService.GetTopTracksAsync())?.ToList() ?? new List<TopTrackDto>();
        for (int i = 0; i < trackDtos.Count; i++)
        {
            var t = trackDtos[i];
            var media = await _mediaRepository.GetByIdAsync(t.Id);
            if (media != null)
                _playbackService.Enqueue(media);
        }
        // Find index of selected track
        var selectedIndex = trackDtos.FindIndex(t => t.Id == dto.Id);
        await _playbackService.PlayAtIndex(selectedIndex);
    }

    [RelayCommand]
    private async Task PlayTopAlbum(TopAlbumDto dto)
    {
        _playbackService.ClearQueue();
        // Загружаем треки для всех связанных альбомов
        var allItems = new List<MediaItem>();
        foreach (var albumId in dto.AlbumIds)
        {
            var albumItems = await _albumRepository.GetSongsForAlbumAsync(albumId);
            allItems.AddRange(albumItems);
        }
        // Убираем возможные дубликаты треков по Id
        var distinctItems = allItems.GroupBy(i => i.Id).Select(g => g.First());
        foreach (var item in distinctItems)
            _playbackService.Enqueue(item);
        await _playbackService.Play();
    }

    [RelayCommand]
    private async Task PlayTopPlaylist(TopPlaylistDto dto)
    {
        _playbackService.ClearQueue();
        var items = await _playlistRepository.GetSongsInPlaylistAsync(dto.Id);
        foreach (var item in items) _playbackService.Enqueue(item);
        await _playbackService.Play();
    }

    [RelayCommand]
    private async Task PlayRecentTrack(RecentTrackDto dto)
    {
        _playbackService.ClearQueue();
        // Enqueue all recent tracks
        var recentDtos = (await _homeService.GetRecentTracksAsync())?.ToList() ?? new List<RecentTrackDto>();
        foreach (var r in recentDtos)
        {
            var item = await _mediaRepository.GetByIdAsync(r.Id);
            if (item != null)
                _playbackService.Enqueue(item);
        }
        // Play the selected recent track at correct position
        var selectedIndex = recentDtos.FindIndex(r => r.Id == dto.Id);
        await _playbackService.PlayAtIndex(selectedIndex);
    }

    [RelayCommand]
    private async Task OpenAlbum(TopAlbumDto dto)
    {
        // Загружаем все треки по всем связанным идентификаторам альбомов
        var tracksList = new List<MediaItem>();
        foreach (var albumId in dto.AlbumIds)
        {
            var albumTracks = await _albumRepository.GetSongsForAlbumAsync(albumId);
            tracksList.AddRange(albumTracks);
        }
        // Убираем дубликаты
        var distinctTracks = tracksList.GroupBy(t => t.Id).Select(g => g.First()).ToList();
        var albumInfo = new AlbumInfo(dto.Title, dto.ArtistName, dto.Year, distinctTracks);
        var albumView = new AlbumView { DataContext = new AlbumViewModel(albumInfo) };
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.DataContext is MainViewModel mainVm)
        {
            mainVm.NavigateTo(albumView, dto.Title);
        }
    }

    [RelayCommand]
    private void OpenPlaylist(TopPlaylistDto dto)
    {
        // Navigate to PlaylistView for selected playlist
        var playlist = new Playlist { Id = dto.Id, Name = dto.Name };
        var playlistView = new PlaylistView { DataContext = new PlaylistViewModel(playlist) };
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.DataContext is MainViewModel mainVm)
        {
            mainVm.NavigateTo(playlistView, dto.Name);
        }
    }
}
