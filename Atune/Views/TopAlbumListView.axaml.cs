using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Shapes;
using System.Windows.Input;
using Atune.Models.Dtos;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Atune.Views
{
    public partial class TopAlbumListView : UserControl
    {
        // Command to play this album
        public static readonly StyledProperty<ICommand?> PlayCommandProperty =
            AvaloniaProperty.Register<TopAlbumListView, ICommand?>(nameof(PlayCommand));
        // Command to open album details
        public static readonly StyledProperty<ICommand?> OpenCommandProperty =
            AvaloniaProperty.Register<TopAlbumListView, ICommand?>(nameof(OpenCommand));

        public ICommand? PlayCommand
        {
            get => GetValue(PlayCommandProperty);
            set => SetValue(PlayCommandProperty, value);
        }
        public ICommand? OpenCommand
        {
            get => GetValue(OpenCommandProperty);
            set => SetValue(OpenCommandProperty, value);
        }

        public TopAlbumListView()
        {
            InitializeComponent();

            var playBtn = this.FindControl<Button>("PlayButton");
            var overlay = this.FindControl<Rectangle>("Overlay");
            var playIcon = this.FindControl<TextBlock>("PlayIcon");
            var openBtn = this.FindControl<Button>("OpenButton");

            if (playBtn != null && overlay != null && playIcon != null)
            {
                playBtn.Click += (_, __) =>
                {
                    if (DataContext is TopAlbumDto dto)
                        PlayCommand?.Execute(dto);
                };

                playBtn.PointerEntered += (_, __) =>
                {
                    if (overlay is not null && Application.Current?.Resources["HoverOpacity"] is double hoverOpacity)
                        overlay.Opacity = hoverOpacity;
                    if (playIcon is not null && Application.Current?.Resources["FullOpacity"] is double fullOpacity)
                        playIcon.Opacity = fullOpacity;
                };
                playBtn.PointerExited += (_, __) =>
                {
                    if (overlay is not null && Application.Current?.Resources["NoOpacity"] is double noOpacity)
                        overlay.Opacity = noOpacity;
                    if (playIcon is not null && Application.Current?.Resources["NoOpacity"] is double noOpacity2)
                        playIcon.Opacity = noOpacity2;
                };
            }
            if (openBtn != null)
            {
                openBtn.Click += (_, __) =>
                {
                    if (DataContext is TopAlbumDto dto)
                        OpenCommand?.Execute(dto);
                };
            }
        }
    }
}