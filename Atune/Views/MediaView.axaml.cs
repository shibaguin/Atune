using Avalonia.Controls;
using Atune.ViewModels;

namespace Atune.Views;

public partial class MediaView : UserControl
{
    public MediaView() => InitializeComponent();
    
    public MediaView(MediaViewModel vm) : this()
    {
        DataContext = vm;
    }
}