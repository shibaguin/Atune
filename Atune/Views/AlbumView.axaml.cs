using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.ViewModels;
using Atune.Models;
using CommunityToolkit.Mvvm.Input;

namespace Atune.Views
{
    public partial class AlbumView : UserControl
    {
        public AlbumView()
        {
            InitializeComponent();
            // Subscribe to mouse and key navigation for Back/Forward
            AddHandler(PointerPressedEvent, OnPointerPressed, handledEventsToo: true);
            AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
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

        private void PlayAlbumButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is AlbumViewModel albumVm)
            {
                var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                    ?.MainWindow?.DataContext as MainViewModel;
                // Enqueue and start playing album without leaving this view
                mainVm?.PlayAlbumCommand.Execute(albumVm.Album);
            }
        }

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
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

        private void OnKeyDown(object sender, KeyEventArgs e)
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
    }
} 