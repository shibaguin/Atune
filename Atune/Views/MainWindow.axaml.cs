using Atune.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Atune.Views;

public partial class MainWindow : Window
{
    // Constructor for XAML
    // Конструктор для XAML
    public MainWindow()
    {
        InitializeComponent();
    }

    // Constructor for DI
    // Конструктор для DI
    public MainWindow(MainViewModel vm) : this()
        => DataContext = vm;
}
