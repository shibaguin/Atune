using Avalonia.Controls;
using Atune.ViewModels;

namespace Atune.Views;

public partial class HomeView : UserControl
{
    // DI constructor: inject HomeViewModel, set DataContext, then initialize view
    public HomeView(HomeViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}

