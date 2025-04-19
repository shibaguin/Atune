using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

        // Command to remove this track; parameter is MediaItem
        public static readonly StyledProperty<ICommand?> RemoveCommandProperty =
            AvaloniaProperty.Register<TrackItemView, ICommand?>(nameof(RemoveCommand));
        public ICommand? RemoveCommand
        {
            get => GetValue(RemoveCommandProperty);
            set => SetValue(RemoveCommandProperty, value);
        }

        public TrackItemView()
        {
            InitializeComponent();

            // Find named controls
            var playBtn = this.FindControl<Button>("PlayButton");
            var menuButton = this.FindControl<Button>("PlaylistMenuButton");
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

            // Wire up remove button and visibility
            if (removeBtn != null)
            {
                removeBtn.Click += (_, __) =>
                    RemoveCommand?.Execute(DataContext);
                this.GetObservable(RemoveCommandProperty).Subscribe(cmd =>
                {
                    removeBtn.IsVisible = cmd != null;
                });
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
            menu.Items = playlists.Select(pl => new MenuItem
            {
                Header = pl.Name,
                Command = AddToPlaylistCommand,
                CommandParameter = pl
            });
        }
    }
} 