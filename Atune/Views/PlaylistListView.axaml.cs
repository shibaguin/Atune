using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Atune.Models;
using Avalonia.Input;
using System.Windows.Input;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.ViewModels;
using System.Linq;

namespace Atune.Views
{
    public partial class PlaylistListView : UserControl
    {
        // OpenCommand for playlist selection
        public static readonly StyledProperty<ICommand?> OpenCommandProperty =
            AvaloniaProperty.Register<PlaylistListView, ICommand?>(nameof(OpenCommand));

        public ICommand? OpenCommand
        {
            get => GetValue(OpenCommandProperty);
            set => SetValue(OpenCommandProperty, value);
        }

        // Compute total duration for display
        public string FormattedDuration
        {
            get
            {
                if (DataContext is Playlist playlist && playlist.PlaylistMediaItems != null)
                {
                    var totalTicks = playlist.PlaylistMediaItems.Sum(pmi => pmi.MediaItem.Duration.Ticks);
                    var dur = TimeSpan.FromTicks(totalTicks);
                    if (dur.Days > 0)
                        return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", dur.Days, dur.Hours, dur.Minutes, dur.Seconds);
                    return string.Format("{0:00}:{1:00}:{2:00}", dur.Hours, dur.Minutes, dur.Seconds);
                }
                return "00:00";
            }
        }

        public PlaylistListView()
        {
            InitializeComponent();

            // Refresh duration when DataContext changes
            this.DataContextChanged += (_, __) => ((Control)this).DataContext = this.DataContext;

            var openBtn = this.FindControl<Button>("OpenButton");
            if (openBtn != null)
            {
                openBtn.Click += OpenBtn_Click;
            }

            var coverButton = this.FindControl<Button>("CoverButton");
            if (coverButton != null)
            {
                coverButton.Click += PlayBtn_Click;
            }

            var overlay = this.FindControl<Rectangle>("Overlay");
            var playIcon = this.FindControl<TextBlock>("PlayIcon");
            if (coverButton != null && overlay != null && playIcon != null)
            {
                coverButton.PointerEntered += (_, __) =>
                {
                    overlay.Opacity = 0.8;
                    playIcon.Opacity = 1;
                };
                coverButton.PointerExited += (_, __) =>
                {
                    overlay.Opacity = 0;
                    playIcon.Opacity = 0;
                };
            }
        }

        private void OpenBtn_Click(object? sender, RoutedEventArgs e)
        {
            OpenCommand?.Execute(DataContext as Playlist);
        }

        private void PlayBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not Playlist playlist) return;

            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                            ?.MainWindow?.DataContext as MainViewModel;
            var mediaVm = mainVm?.MediaViewModelInstance;
            if (mediaVm == null) return;

            mediaVm.ClearQueueCommand.Execute(null);
            foreach (var pmi in playlist.PlaylistMediaItems.OrderBy(p => p.Position))
            {
                mediaVm.AddToQueueCommand.Execute(pmi.MediaItem);
            }
            mediaVm.PlayNextInQueueCommand.Execute(null);
        }
    }
}
