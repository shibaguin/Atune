using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Atune.ViewModels;

namespace Atune.Views;

public partial class MediaView : UserControl
{
    public MediaView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.GetService<MediaViewModel>();
    }
}