using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Shapes;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using Atune.Models.Dtos;

namespace Atune.Views
{
    public partial class TopTrackListView : UserControl
    {
        public static readonly StyledProperty<ICommand?> PlayCommandProperty =
            AvaloniaProperty.Register<TopTrackListView, ICommand?>(nameof(PlayCommand));

        public ICommand? PlayCommand
        {
            get => GetValue(PlayCommandProperty);
            set => SetValue(PlayCommandProperty, value);
        }

        public TopTrackListView()
        {
            InitializeComponent();

            var playBtn = this.FindControl<Button>("PlayButton");
            var overlay = this.FindControl<Rectangle>("Overlay");
            var playIcon = this.FindControl<TextBlock>("PlayIcon");

            if (playBtn != null)
            {
                playBtn.Click += (_, __) =>
                {
                    if (DataContext is TopTrackDto dto)
                    {
                        PlayCommand?.Execute(dto);
                    }
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
        }
    }
}