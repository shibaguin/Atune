using Avalonia;
using Atune.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls;

namespace Atune.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = ServiceLocator.GetService<MainViewModel>();
    }
}