using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Atune.ViewModels;

namespace Atune.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        DataContext = ServiceLocator.GetService<HomeViewModel>();
    }
}