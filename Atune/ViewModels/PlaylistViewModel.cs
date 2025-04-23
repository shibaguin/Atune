using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.Models;
using Atune.Services;
using System.Linq;
using System;
using System.Collections.Specialized;

namespace Atune.ViewModels
{
    public partial class PlaylistViewModel : ObservableObject
    {
        public Playlist Playlist { get; }
        private readonly IPlaylistService _playlistService;
        public ObservableCollection<MediaItem> Tracks { get; } = new ObservableCollection<MediaItem>();

        public PlaylistViewModel(Playlist playlist)
        {
            Playlist = playlist;
            _playlistService = App.Current!.Services!.GetRequiredService<IPlaylistService>();
            Name = playlist.Name;
            _ = LoadTracksAsync();
            Tracks.CollectionChanged += (_, __) => OnPropertyChanged(nameof(TotalDuration));
        }

        [ObservableProperty]
        private string name;

        public string TotalDuration
        {
            get
            {
                var dur = TimeSpan.FromTicks(Tracks.Sum(t => t.Duration.Ticks));
                if (dur.Days > 0)
                    return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", dur.Days, dur.Hours, dur.Minutes, dur.Seconds);
                return string.Format("{0:00}:{1:00}:{2:00}", dur.Hours, dur.Minutes, dur.Seconds);
            }
        }

        [RelayCommand]
        public async Task LoadTracksAsync()
        {
            Tracks.Clear();
            var songs = await _playlistService.GetSongsInPlaylistAsync(Playlist.Id);
            foreach (var s in songs)
                Tracks.Add(s);
        }

        [RelayCommand]
        public async Task RenameAsync()
        {
            if (Name != Playlist.Name)
            {
                await _playlistService.RenamePlaylistAsync(Playlist.Id, Name);
                Playlist.Name = Name;
            }
        }

        [RelayCommand]
        public void GoBack()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                var mainVm = lifetime.MainWindow?.DataContext as MainViewModel;
                mainVm?.GoMediaCommand.Execute(null);
            }
        }

        [RelayCommand]
        public async Task DeleteAsync()
        {
            await _playlistService.DeletePlaylistAsync(Playlist.Id);
            GoBack();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                var mainVm = lifetime.MainWindow?.DataContext as MainViewModel;
                if (mainVm?.CurrentView is Avalonia.Controls.UserControl view && view.DataContext is MediaViewModel mediaVm)
                {
                    var toRemove = mediaVm.Playlists.FirstOrDefault(p => p.Id == Playlist.Id);
                    if (toRemove != null)
                        mediaVm.Playlists.Remove(toRemove);
                }
            }
        }

        [RelayCommand]
        public async Task RemoveTrackAsync(MediaItem track)
        {
            if (track == null) return;
            await _playlistService.RemoveFromPlaylistAsync(Playlist.Id, track.Id);
            await LoadTracksAsync();
        }

        [RelayCommand]
        public async Task PlayAllAsync()
        {
            var service = App.Current!.Services!.GetRequiredService<IPlayPlaylistService>();
            await service.PlayPlaylistAsync(Playlist);
        }
    }
} 