using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Atune.Models;
using Atune.ViewModels;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia;
using System.ComponentModel;
using System;
using Avalonia.Threading;
using Atune.Services;
using System.Linq;
using System.Collections.Generic;

namespace Atune.Views;


public partial class MainView : UserControl
{
    // Custom progress bar elements
    private readonly Border? _progressBarBackground;
    private readonly Rectangle? _progressBarFill;
    public MainView()
    {
        InitializeComponent();
        // Find custom progress bar elements
        _progressBarBackground = this.FindControl<Border>("ProgressBarBackground");
        _progressBarFill = this.FindControl<Rectangle>("ProgressBarFill");
        if (_progressBarBackground != null)
        {
            _progressBarBackground.PointerPressed += OnProgressBarPointer;
            _progressBarBackground.PointerMoved += OnProgressBarPointer;
        }
        // Subscribe to ViewModel changes
        DataContextChanged += (s, e) =>
        {
            if (DataContext is INotifyPropertyChanged vm)
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;
                // Initial update
                UpdateCustomProgressBar();
            }
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentPosition) ||
            e.PropertyName == nameof(MainViewModel.Duration))
        {
            Dispatcher.UIThread.Post(UpdateCustomProgressBar);
        }
    }

    private void UpdateCustomProgressBar()
    {
        if (_progressBarBackground == null || _progressBarFill == null)
            return;

        if (DataContext is MainViewModel vm && vm.Duration.TotalSeconds > 0)
        {
            var backgroundWidth = _progressBarBackground.Bounds.Width;
            if (backgroundWidth <= 0)
                return;
            double ratio = vm.CurrentPosition.TotalSeconds / vm.Duration.TotalSeconds;
            ratio = Math.Clamp(ratio, 0, 1);
            _progressBarFill.Width = ratio * backgroundWidth;
        }
    }

    private void OnProgressBarPointer(object? sender, PointerEventArgs e)
    {
        if (_progressBarBackground == null || DataContext is not MainViewModel vm)
            return;
        var props = e.GetCurrentPoint(_progressBarBackground).Properties;
        if (props.IsLeftButtonPressed || e is PointerPressedEventArgs)
        {
            var pos = e.GetPosition(_progressBarBackground);
            double ratio = pos.X / _progressBarBackground.Bounds.Width;
            var newTime = TimeSpan.FromSeconds(Math.Clamp(ratio, 0, 1) * vm.Duration.TotalSeconds);
            vm.CurrentPosition = newTime;
        }
    }

    private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SearchCommand.Execute(null);
            }
        }
    }


    private void SearchSuggestions_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is string selectedSuggestion &&
            DataContext is MainViewModel vm)
        {
            listBox.SelectedItem = null;
            vm.SearchQuery = selectedSuggestion;
            vm.IsSuggestionsOpen = false;
        }
    }

    public void PlayButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PlayCommand.Execute(null);
        }
    }

    public void StopButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.StopCommand.Execute(null);
        }
    }

    // XAML event handlers for pointer events
    public void ProgressBarBackground_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        => OnProgressBarPointer(sender, e);
    public void ProgressBarBackground_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        => OnProgressBarPointer(sender, e);

    private async void SearchResultsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is Atune.Services.SearchResult result
            && DataContext is MainViewModel vm)
        {
            // Close the popup and clear selection
            vm.Search.IsOpen = false;
            listBox.SelectedItem = null;

            switch (result.Category)
            {
                case "Navigation":
                    switch (result.Title)
                    {
                        case "Home": vm.GoHomeCommand.Execute(null); break;
                        case "Media": vm.GoMediaCommand.Execute(null); break;
                        case "History": vm.GoHistoryCommand.Execute(null); break;
                        case "Settings": vm.GoSettingsCommand.Execute(null); break;
                    }
                    break;

                case "Albums":
                    if (result.Data is AlbumInfo album)
                    {
                        var view = new AlbumView { DataContext = new AlbumViewModel(album) };
                        vm.SelectedSection = MainViewModel.SectionType.Media;
                        vm.CurrentView = view;
                        vm.HeaderText = album.AlbumName;
                    }
                    break;

                case "Tracks":
                    vm.GoMediaCommand.Execute(null);
                    if (vm.MediaViewModelInstance is var mediaVm && result.Data is MediaItem mi)
                    {
                        mediaVm?.PlayTrackCommand.Execute(mi);
                    }
                    break;

                case "Playlists":
                    if (result.Data is Playlist pl)
                        vm.GoPlaylistCommand.Execute(pl);
                    break;

                case "Settings":
                    vm.GoSettingsCommand.Execute(null);
                    // Additional settings selection logic can go here
                    break;

                case "Artists":
                    if (result.Data is ArtistInfo artistInfo)
                    {
                        // Switch to Media section, refresh data and open the artist (ensures full metadata)
                        vm.GoMediaCommand.Execute(null);
                        var artistMediaVm = vm.MediaViewModelInstance;
                        if (artistMediaVm != null)
                        {
                            // Reload all media (tracks, albums, artists)
                            await artistMediaVm.RefreshMediaCommand.ExecuteAsync(null);
                            // Find the matching cached ArtistInfo
                            var existing = artistMediaVm.Artists.FirstOrDefault(a => string.Equals(a.ArtistName, artistInfo.ArtistName, StringComparison.OrdinalIgnoreCase))
                                ?? artistMediaVm.Artists.FirstOrDefault(a => string.Equals(a.ArtistName.Replace('-', '/'), artistInfo.ArtistName, StringComparison.OrdinalIgnoreCase))
                                ?? artistMediaVm.Artists.FirstOrDefault(a => string.Equals(a.ArtistName, artistInfo.ArtistName.Replace('/', '-'), StringComparison.OrdinalIgnoreCase));
                            // Execute open using the cached or fallback object
                            artistMediaVm.OpenArtistCommand.Execute(existing ?? artistInfo);
                        }
                    }
                    break;
            }
        }
    }

    // Handle clicks on search result buttons to navigate
    private async void SearchResultButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Atune.Services.SearchResult result
            && DataContext is MainViewModel vm)
        {
            // Close the popup
            vm.Search.IsOpen = false;

            switch (result.Category)
            {
                case "Navigation":
                    switch (result.Title)
                    {
                        case "Home": vm.GoHomeCommand.Execute(null); break;
                        case "Media": vm.GoMediaCommand.Execute(null); break;
                        case "History": vm.GoHistoryCommand.Execute(null); break;
                        case "Settings": vm.GoSettingsCommand.Execute(null); break;
                    }
                    break;

                case "Albums":
                    AlbumInfo? albumToShow = null;
                    if (result.Data is AlbumInfo ai)
                    {
                        albumToShow = ai;
                    }
                    else if (result.Data is Album dbAlbum)
                    {
                        // Convert DB Album to AlbumInfo
                        var artistName = dbAlbum.AlbumArtists?.FirstOrDefault()?.Artist.Name ?? string.Empty;
                        var tracks = dbAlbum.Tracks?.ToList() ?? [];
                        albumToShow = new AlbumInfo(dbAlbum.Title, artistName, (uint)dbAlbum.Year, tracks);
                    }
                    if (albumToShow != null)
                    {
                        var view = new AlbumView { DataContext = new AlbumViewModel(albumToShow) };
                        vm.SelectedSection = MainViewModel.SectionType.Media;
                        vm.CurrentView = view;
                        vm.HeaderText = albumToShow.AlbumName;
                    }
                    break;

                case "Tracks":
                    vm.GoMediaCommand.Execute(null);
                    if (vm.MediaViewModelInstance is var mediaVmBtn && result.Data is MediaItem mi)
                        mediaVmBtn?.PlayTrackCommand.Execute(mi);
                    break;

                case "Playlists":
                    if (result.Data is Playlist pl)
                        vm.GoPlaylistCommand.Execute(pl);
                    break;

                case "Artists":
                    if (result.Data is ArtistInfo artistInfoBtn)
                    {
                        // Switch to Media section
                        vm.GoMediaCommand.Execute(null);
                        var artistBtnVm = vm.MediaViewModelInstance;
                        if (artistBtnVm != null)
                        {
                            // Ensure latest media loaded
                            await artistBtnVm.RefreshMediaCommand.ExecuteAsync(null);
                            // Find in cached artists and open
                            var cached = artistBtnVm.Artists.FirstOrDefault(a => string.Equals(a.ArtistName, artistInfoBtn.ArtistName, StringComparison.OrdinalIgnoreCase))
                                ?? artistBtnVm.Artists.FirstOrDefault(a => string.Equals(a.ArtistName.Replace('-', '/'), artistInfoBtn.ArtistName, StringComparison.OrdinalIgnoreCase))
                                ?? artistBtnVm.Artists.FirstOrDefault(a => string.Equals(a.ArtistName, artistInfoBtn.ArtistName.Replace('/', '-'), StringComparison.OrdinalIgnoreCase));
                            artistBtnVm.OpenArtistCommand.Execute(cached ?? artistInfoBtn);
                        }
                    }
                    break;
            }
        }
    }
}
