using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Atune.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Atune.Views;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
        DataContext = App.Current?.Services?.GetService<HistoryViewModel>();
    }
}