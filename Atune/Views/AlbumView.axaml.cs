using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Atune.Views
{
    public partial class AlbumView : Window
    {
        public AlbumView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 