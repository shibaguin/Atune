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
    public partial class PlaylistView : UserControl
    {
        public PlaylistView()
        {
            InitializeComponent();
            // Subscribe to MainViewModel changes to update track selection
            var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            // Find the track list control
            var trackList = this.FindControl<ListBox>("TrackList");
            if (desktop?.MainWindow?.DataContext is MainViewModel mainVm && trackList != null)
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

        // Expose MainViewModel's PlayAlbumFromTrackCommand for TrackListView
        public static IRelayCommand<MediaItem?>? PlayTrackCommand
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

        private void GoBack_Click(object? sender, RoutedEventArgs e)
        {
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            if (mainVm != null)
            {
                mainVm.NavigateBack();
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var kind = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            if (kind == PointerUpdateKind.XButton1Pressed)
            {
                if (mainVm != null)
                {
                    mainVm.NavigateBack();
                }
                e.Handled = true;
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            if (e.Key == Key.Escape || e.Key == Key.BrowserBack)
            {
                if (mainVm != null)
                {
                    mainVm.NavigateBack();
                }
                e.Handled = true;
            }
        }

        private void OnTrackListLayoutUpdated(object? sender, EventArgs e)
        {
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            if (mainVm?.CurrentMediaItem != null)
            {
                var trackList = this.FindControl<ListBox>("TrackList");
                if (trackList != null)
                {
                    trackList.SelectedItem = mainVm.CurrentMediaItem;
                }
            }
        }

        private void OnTitleTextPressed(object? sender, PointerPressedEventArgs e)
        {
            var titleText = this.FindControl<TextBlock>("TitleText");
            var titleBox = this.FindControl<TextBox>("TitleBox");
            if (titleText != null && titleBox != null)
            {
                titleText.IsVisible = false;
                titleBox.IsVisible = true;
                titleBox.Focus();
            }
        }

        private void OnTitleBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var titleText = this.FindControl<TextBlock>("TitleText");
                var titleBox = this.FindControl<TextBox>("TitleBox");
                if (titleText != null && titleBox != null)
                {
                    titleText.IsVisible = true;
                    titleBox.IsVisible = false;
                }
            }
        }

        private void OnTitleBoxLostFocus(object? sender, RoutedEventArgs e)
        {
            var titleText = this.FindControl<TextBlock>("TitleText");
            var titleBox = this.FindControl<TextBox>("TitleBox");
            if (titleText != null && titleBox != null)
            {
                titleText.IsVisible = true;
                titleBox.IsVisible = false;
            }
        }
    }
}
