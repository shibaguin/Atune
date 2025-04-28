using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Shapes;
using System.Windows.Input;
using Atune.Models.Dtos;

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

            if (playBtn != null)
            {
                playBtn.Click += (_, __) =>
                {
                    if (DataContext is TopAlbumDto dto)
                        PlayCommand?.Execute(dto);
                };

                if (overlay != null && playIcon != null)
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