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

namespace Atune.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<HomeViewModel> _logger;
    private readonly IHomeRepository _homeRepository;

    [ObservableProperty]
    private string _welcomeMessage = string.Empty;

    public ObservableCollection<TopTrackDto> TopTracks { get; } = new();
    public ObservableCollection<TopAlbumDto> TopAlbums { get; } = new();
    public ObservableCollection<TopPlaylistDto> TopPlaylists { get; } = new();
    public ObservableCollection<RecentTrackDto> RecentTracks { get; } = new();

    public HomeViewModel(
        IMemoryCache cache,
        ILogger<HomeViewModel> logger,
        IHomeRepository homeRepository)
    {
        _homeRepository = homeRepository;
        _cache = cache;
        _logger = logger;
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
            var tracks = await _homeRepository.GetTopTracksAsync();
            _logger.LogInformation("HomeViewModel: Loaded TopTracks count = {Count}", tracks?.Count() ?? 0);
            Dispatcher.UIThread.Post(() =>
            {
                TopTracks.Clear();
                foreach (var t in tracks) TopTracks.Add(t);
            });

            var albums = await _homeRepository.GetTopAlbumsAsync();
            _logger.LogInformation("HomeViewModel: Loaded TopAlbums count = {Count}", albums?.Count() ?? 0);
            Dispatcher.UIThread.Post(() =>
            {
                TopAlbums.Clear();
                foreach (var a in albums) TopAlbums.Add(a);
            });

            var playlists = await _homeRepository.GetTopPlaylistsAsync();
            _logger.LogInformation("HomeViewModel: Loaded TopPlaylists count = {Count}", playlists?.Count() ?? 0);
            Dispatcher.UIThread.Post(() =>
            {
                TopPlaylists.Clear();
                foreach (var p in playlists) TopPlaylists.Add(p);
            });

            var recents = await _homeRepository.GetRecentTracksAsync();
            _logger.LogInformation("HomeViewModel: Loaded RecentTracks count = {Count}", recents?.Count() ?? 0);
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
    private void PlayTopTrack(TopTrackDto dto)
    {
        // TODO: Implement play logic for top track
    }

    [RelayCommand]
    private void PlayTopAlbum(TopAlbumDto dto)
    {
        // TODO: Implement play logic for top album
    }

    [RelayCommand]
    private void PlayTopPlaylist(TopPlaylistDto dto)
    {
        // TODO: Implement play logic for top playlist
    }

    [RelayCommand]
    private void PlayRecentTrack(RecentTrackDto dto)
    {
        // TODO: Implement play logic for recent track
    }
}
