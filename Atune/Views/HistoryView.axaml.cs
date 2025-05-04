using Avalonia.Controls;
using Atune.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;

namespace Atune.Views;

public partial class HistoryView : UserControl
{
    // Проксируем команду из VM для биндинга в XAML
    public static readonly StyledProperty<ICommand?> PlayRecentTrackCommandProperty =
        AvaloniaProperty.Register<HistoryView, ICommand?>(nameof(PlayRecentTrackCommand));

    public ICommand? PlayRecentTrackCommand
    {
        get => GetValue(PlayRecentTrackCommandProperty);
        set => SetValue(PlayRecentTrackCommandProperty, value);
    }

    public HistoryView()
    {
        InitializeComponent();
    }

    public HistoryView(HistoryViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        // Устанавливаем команду для RecentTrackView
        PlayRecentTrackCommand = viewModel.PlayRecentTrackCommand;
    }
}
