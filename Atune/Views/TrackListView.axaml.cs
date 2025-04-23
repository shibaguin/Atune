using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Windows.Input;
using Atune.Models;
using Microsoft.Extensions.DependencyInjection;
using Atune.Services;
using System.Collections.Generic;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.ViewModels;
using Avalonia.Input;
using Avalonia.Controls.Shapes;

namespace Atune.Views
{
    public partial class TrackListView : UserControl
    {
        // Command to play this track
        public static readonly StyledProperty<ICommand?> PlayCommandProperty =
            AvaloniaProperty.Register<TrackListView, ICommand?>(nameof(PlayCommand));
        public ICommand? PlayCommand
        {
            get => GetValue(PlayCommandProperty);
            set => SetValue(PlayCommandProperty, value);
        }

        // Command to add this track to a playlist; parameter is Playlist
        public static readonly StyledProperty<ICommand?> AddToPlaylistCommandProperty =
            AvaloniaProperty.Register<TrackListView, ICommand?>(nameof(AddToPlaylistCommand));
        public ICommand? AddToPlaylistCommand
        {
            get => GetValue(AddToPlaylistCommandProperty);
            set => SetValue(AddToPlaylistCommandProperty, value);
        }

        public TrackListView()
        {
            InitializeComponent();

            // Find named controls
            var playBtn = this.FindControl<Button>("PlayButton");
            var menuButton = this.FindControl<Button>("PlaylistMenuButton");
            var playlistMenu = this.FindControl<ContextMenu>("PlaylistContextMenu");
            var removeBtn = this.FindControl<Button>("RemoveButton");

            // Wire up play button (fallback to MediaPlayerService if no PlayCommand)
            if (playBtn != null)
            {
                playBtn.Click += async (_, __) =>
                {
                    if (DataContext is MediaItem item)
                    {
                        var player = App.Current!.Services!.GetRequiredService<MediaPlayerService>();
                        // If this track was paused, resume
                        if (player.CurrentPath != null && !player.IsPlaying)
                        {
                            try
                            {
                                var uri = new Uri(player.CurrentPath);
                                if (uri.IsFile && uri.LocalPath == item.Path)
                                {
                                    player.Resume();
                                    return;
                                }
                            }
                            catch
                            {
                                // ignore URI parsing errors
                            }
                        }
                        // Otherwise start fresh play
                        if (PlayCommand != null)
                        {
                            PlayCommand.Execute(item);
                        }
                        else
                        {
                            await player.StopAsync();
                            await player.Play(item.Path);
                        }
                    }
                };
            }

            // Wire up hover overlay and play icon visibility
            var overlay = this.FindControl<Rectangle>("Overlay");
            var playIcon = this.FindControl<TextBlock>("PlayIcon");
            if (playBtn != null && overlay != null && playIcon != null)
            {
                playBtn.PointerEntered += (_, __) =>
                {
                    overlay.Opacity = 0.8;
                    playIcon.Opacity = 1;
                };
                playBtn.PointerExited += (_, __) =>
                {
                    overlay.Opacity = 0;
                    playIcon.Opacity = 0;
                };
            }

            // Wire up playlist menu button to open the context menu
            if (menuButton != null && playlistMenu != null)
            {
                menuButton.Click += (_, __) => playlistMenu.Open(menuButton);
                // Populate playlists menu when opened
                playlistMenu.Opened += PlaylistContextMenu_Opened;
            }

            // Show and wire up remove button only in PlaylistView context
            if (removeBtn != null)
            {
                // Show only when inside a PlaylistView
                var isInPlaylist = this.GetVisualAncestors()
                                   .OfType<Atune.Views.PlaylistView>()
                                   .Any();
                removeBtn.IsVisible = isInPlaylist;
                removeBtn.Click += (_, __) =>
                {
                    if (this.DataContext is Atune.Models.MediaItem item)
                    {
                        var playlistView = this.GetVisualAncestors()
                                               .OfType<Atune.Views.PlaylistView>()
                                               .FirstOrDefault();
                        if (playlistView?.DataContext is Atune.ViewModels.PlaylistViewModel pvm)
                        {
                            pvm.RemoveTrackCommand.Execute(item);
                        }
                    }
                };
            }
        }

        private async void PlaylistContextMenu_Opened(object? sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu menu)
                return;

            // Retrieve all playlists from the service
            var service = App.Current!.Services!.GetRequiredService<IPlaylistService>();
            var playlists = await service.GetPlaylistsAsync();

            // Build menu items with click handlers to add current track
            var items = new List<MenuItem>();
            foreach (var pl in playlists)
            {
                var mi = new MenuItem { Header = pl.Name };
                mi.Click += async (_, __) =>
                {
                    if (DataContext is MediaItem track)
                    {
                        try
                        {
                            await service.AddToPlaylistAsync(pl.Id, track.Id);
                        }
                        catch
                        {
                            // Optionally log error
                        }
                    }
                };
                items.Add(mi);
            }
            menu.ItemsSource = items;
        }
    }
} 
