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

namespace Atune.Views
{
    public partial class TrackItemView : UserControl
    {
        // Command to play this track
        public static readonly StyledProperty<ICommand?> PlayCommandProperty =
            AvaloniaProperty.Register<TrackItemView, ICommand?>(nameof(PlayCommand));
        public ICommand? PlayCommand
        {
            get => GetValue(PlayCommandProperty);
            set => SetValue(PlayCommandProperty, value);
        }

        // Command to add this track to a playlist; parameter is Playlist
        public static readonly StyledProperty<ICommand?> AddToPlaylistCommandProperty =
            AvaloniaProperty.Register<TrackItemView, ICommand?>(nameof(AddToPlaylistCommand));
        public ICommand? AddToPlaylistCommand
        {
            get => GetValue(AddToPlaylistCommandProperty);
            set => SetValue(AddToPlaylistCommandProperty, value);
        }

        public TrackItemView()
        {
            InitializeComponent();

            // Find named controls
            var playBtn = this.FindControl<Button>("PlayButton");
            var playlistMenu = this.FindControl<ContextMenu>("PlaylistContextMenu");
            var removeBtn = this.FindControl<Button>("RemoveButton");

            // Wire up play button
            if (playBtn != null)
            {
                playBtn.Click += (_, __) =>
                    PlayCommand?.Execute(DataContext);
            }

            // Populate playlists menu when opened
            if (playlistMenu != null)
            {
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
            if (sender is not ContextMenu menu || AddToPlaylistCommand == null)
                return;

            // Retrieve all playlists from the service
            var service = App.Current.Services.GetRequiredService<IPlaylistService>();
            var playlists = await service.GetPlaylistsAsync();

            // Build menu items
            menu.ItemsSource = playlists.Select(pl => new MenuItem
            {
                Header = pl.Name,
                Command = AddToPlaylistCommand,
                CommandParameter = pl
            });
        }
    }
} 