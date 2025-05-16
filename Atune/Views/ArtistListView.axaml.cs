using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.Models;
using Atune.Services;
using Atune.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Styling;

namespace Atune.Views
{
    public partial class ArtistListView : UserControl
    {
        // Command to open artist details
        public static readonly StyledProperty<ICommand?> OpenCommandProperty =
            AvaloniaProperty.Register<ArtistListView, ICommand?>(nameof(OpenCommand));

        public ICommand? OpenCommand
        {
            get => GetValue(OpenCommandProperty);
            set => SetValue(OpenCommandProperty, value);
        }

        public ArtistListView()
        {
            InitializeComponent();

            var openBtn = this.FindControl<Button>("OpenButton");
            if (openBtn != null)
                openBtn.Click += OpenBtn_Click;

            var coverButton = this.FindControl<Button>("CoverButton");
            var overlay = this.FindControl<Rectangle>("Overlay");
            var playIcon = this.FindControl<TextBlock>("PlayIcon");
            if (coverButton != null)
                coverButton.Click += PlayBtn_Click;
            if (coverButton != null && overlay != null && playIcon != null)
            {
                coverButton.PointerEntered += (_, __) =>
                {
                    if (overlay is not null && Application.Current?.Resources["HoverOpacity"] is double hoverOpacity)
                        overlay.Opacity = hoverOpacity;
                    if (playIcon is not null && Application.Current?.Resources["FullOpacity"] is double fullOpacity)
                        playIcon.Opacity = fullOpacity;
                };
                coverButton.PointerExited += (_, __) =>
                {
                    if (overlay is not null && Application.Current?.Resources["NoOpacity"] is double noOpacity)
                        overlay.Opacity = noOpacity;
                    if (playIcon is not null && Application.Current?.Resources["NoOpacity"] is double noOpacity2)
                        playIcon.Opacity = noOpacity2;
                };
            }
        }

        private void OpenBtn_Click(object? sender, RoutedEventArgs e)
        {
            OpenCommand?.Execute(DataContext as ArtistInfo);
        }

        private async void PlayBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not ArtistInfo artist) return;
            var service = App.Current!.Services!.GetRequiredService<IPlayArtistService>();
            await service.PlayArtistAsync(artist);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
