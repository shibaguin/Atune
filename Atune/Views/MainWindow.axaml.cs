using Atune.ViewModels;
using Avalonia.Controls;

namespace Atune.Views;

public partial class MainWindow : Window
{
    // Конструктор для XAML
    public MainWindow()
    {
        InitializeComponent();
    }
    
    // Конструктор для DI
    public MainWindow(MainViewModel vm) : this()
    {
        DataContext = vm;
    }
}