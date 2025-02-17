using Atune.ViewModels;
using Avalonia.Controls;

namespace Atune.Views;

public partial class MainWindow : Window
{
    // Constructor for XAML
    public MainWindow()
    {
        InitializeComponent();
    }
    
    // Constructor for DI
    public MainWindow(MainViewModel vm) : this()
    {
        DataContext = vm;
    }
}