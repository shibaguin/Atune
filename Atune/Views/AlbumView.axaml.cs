using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.ViewModels;
using Atune.Models;
using CommunityToolkit.Mvvm.Input;
using Atune.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using Avalonia.Threading;
using System;

namespace Atune.Views
{
    public partial class AlbumView : UserControl
    {
        public AlbumView()
        {
            InitializeComponent();
            // Subscribe to MainViewModel changes to update track selection
            var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            // Find the track list control
            var trackList = this.FindControl<ListBox>("TrackList");
            var mainVm = desktop?.MainWindow?.DataContext as MainViewModel;
            if (mainVm != null && trackList != null)
            {
                // Subscribe to updates
                mainVm.PropertyChanged += OnMainVmPropertyChanged;
                // Highlight the current item after the ListBox lays out its items
                trackList.LayoutUpdated += OnTrackListLayoutUpdated;
            }
            // Subscribe to mouse and key navigation for Back/Forward
            AddHandler(PointerPressedEvent, OnPointerPressed, handledEventsToo: true);
            AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
        }

        private void OnMainVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentMediaItem) && sender is MainViewModel vm)
            {
                var trackList = this.FindControl<ListBox>("TrackList");
                Dispatcher.UIThread.Post(() =>
                {
                    if (trackList != null)
                        trackList.SelectedItem = vm.CurrentMediaItem;
                });
            }
        }

        // Expose MainViewModel's PlayAlbumFromTrackCommand for TrackItemView
        public IAsyncRelayCommand<MediaItem>? PlayTrackCommand
        {
            get
            {
                var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                               ?.MainWindow?.DataContext as MainViewModel;
                return mainVm?.PlayAlbumFromTrackCommand;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            // Navigate back to Media view
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            mainVm?.GoMediaCommand.Execute(null);
        }

        private async void PlayAlbumButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is AlbumViewModel albumVm)
            {
                // Delegate album-play logic to PlayAlbumService
                var service = App.Current.Services.GetRequiredService<IPlayAlbumService>();
                await service.PlayAlbumAsync(albumVm.Album);
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var kind = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            if (kind == PointerUpdateKind.XButton1Pressed)
            {
                mainVm?.GoMediaCommand.Execute(null);
                e.Handled = true;
            }
            else if (kind == PointerUpdateKind.XButton2Pressed)
            {
                // Implement Forward navigation if needed
                e.Handled = true;
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            if (e.Key == Key.Escape || e.Key == Key.BrowserBack)
            {
                mainVm?.GoMediaCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.BrowserForward)
            {
                // Forward navigation if needed
                e.Handled = true;
            }
        }

        private void TrackButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MediaItem track)
            {
                var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                    ?.MainWindow?.DataContext as MainViewModel;
                mainVm?.PlayAlbumFromTrackCommand.Execute(track);
            }
        }

        private void OnTrackListLayoutUpdated(object? sender, EventArgs e)
        {
            if (sender is ListBox list &&
                Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow?.DataContext is MainViewModel mainVm)
            {
                list.SelectedItem = mainVm.CurrentMediaItem;
            }
            // Unsubscribe so it only runs once
            if (sender is ListBox lb)
                lb.LayoutUpdated -= OnTrackListLayoutUpdated;
        }
    }
} 