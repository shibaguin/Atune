using Avalonia.Controls;
using Atune.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Atune.Views;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
    }

    public HistoryView(HistoryViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}
