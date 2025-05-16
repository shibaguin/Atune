using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Shapes;
using System.Windows.Input;
using Atune.Models.Dtos;

namespace Atune.Views
{
    public partial class RecentlyPlayedListView : UserControl
    {
        // Command to play this recent track
        public static readonly StyledProperty<ICommand?> PlayCommandProperty =
            AvaloniaProperty.Register<RecentlyPlayedListView, ICommand?>(nameof(PlayCommand));

        public ICommand? PlayCommand
        {
            get => GetValue(PlayCommandProperty);
            set => SetValue(PlayCommandProperty, value);
        }

        public RecentlyPlayedListView()
        {
            InitializeComponent();

            var playBtn = this.FindControl<Button>("PlayButton");
            var overlay = this.FindControl<Rectangle>("Overlay");
            var playIcon = this.FindControl<TextBlock>("PlayIcon");

            if (playBtn != null)
            {
                playBtn.Click += (_, __) =>
                {
                    if (DataContext is RecentTrackDto dto)
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
        }
    }
}