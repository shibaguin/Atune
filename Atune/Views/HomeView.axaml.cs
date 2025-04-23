using Avalonia.Controls;
using Atune.ViewModels;

namespace Atune.Views;

public partial class HomeView : UserControl
{
    public HomeView() => InitializeComponent();
    
    public HomeView(HomeViewModel vm) : this()
    {
        DataContext = vm;
    }
}
