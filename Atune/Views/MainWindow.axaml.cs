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
        // Tunnel KeyUp event to catch Spacebar before any child control handles it
        this.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
    }
    
    // Constructor for DI
    // Конструктор для DI
    public MainWindow(MainViewModel vm) : this()
        => DataContext = vm;

    // Handler for Spacebar key up to toggle play/pause
    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && !(e.Source is TextBox) && DataContext is MainViewModel vm)
        {
            vm.TogglePlayPauseCommand.Execute(null);
            e.Handled = true;
        }
    }
}