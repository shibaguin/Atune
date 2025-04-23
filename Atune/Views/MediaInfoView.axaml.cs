using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Atune.Models;
using Serilog;

namespace Atune.Views
{
    public partial class MediaInfoView : UserControl
    {
        public MediaInfoView()
        {
            InitializeComponent();
        }

        public MediaInfoView(MediaItem mediaItem) : this()
        {
            DataContext = mediaItem;
            if (mediaItem == null)
            {
                Log.Warning("MediaItem is null");
            }
            else
            {
                Log.Information($"MediaItem: {mediaItem.Title}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 
