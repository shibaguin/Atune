using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Shapes;
using System.Windows.Input;
using Atune.Models.Dtos;

namespace Atune.Views
{
    public partial class TopPlaylistListView : UserControl
    {
        // Command to play this playlist
        public static readonly StyledProperty<ICommand?> PlayCommandProperty =
            AvaloniaProperty.Register<TopPlaylistListView, ICommand?>(nameof(PlayCommand));
        // Command to open playlist details
        public static readonly StyledProperty<ICommand?> OpenCommandProperty =
            AvaloniaProperty.Register<TopPlaylistListView, ICommand?>(nameof(OpenCommand));

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

        public TopPlaylistListView()
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
                    if (DataContext is TopPlaylistDto dto)
                        PlayCommand?.Execute(dto);
                };

                if (overlay != null && playIcon != null)
                {
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
            }
            if (openBtn != null)
            {
                openBtn.Click += (_, __) =>
                {
                    if (DataContext is TopPlaylistDto dto)
                        OpenCommand?.Execute(dto);
                };
            }
        }
    }
}