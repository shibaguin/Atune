using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Atune.Models;
using Avalonia.Input;
using Avalonia.Controls.Shapes;
using System.Windows.Input;
using Atune.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Atune.Views
{
    public partial class AlbumListView : UserControl
    {
        // OpenCommand for album selection
        public static readonly StyledProperty<ICommand?> OpenCommandProperty =
            AvaloniaProperty.Register<AlbumListView, ICommand?>(nameof(OpenCommand));

        public ICommand? OpenCommand
        {
            get => GetValue(OpenCommandProperty);
            set => SetValue(OpenCommandProperty, value);
        }

        public AlbumListView()
        {
            InitializeComponent();

            var openBtn = this.FindControl<Button>("OpenButton");
            if (openBtn != null)
            {
                openBtn.Click += OpenBtn_Click;
            }
            // Hook CoverButton click to PlayAlbumService
            var coverButton = this.FindControl<Button>("CoverButton");
            if (coverButton != null)
            {
                coverButton.Click += PlayBtn_Click;
            }

            // Attach pointer-entered/exited to dim overlay and show play icon
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
            OpenCommand?.Execute(DataContext as AlbumInfo);
        }

        private async void PlayBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not AlbumInfo album) return;
            var service = App.Current!.Services!.GetRequiredService<IPlayAlbumService>();
            await service.PlayAlbumAsync(album);
        }
    }
}
