using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.ViewModels;
using Avalonia;

namespace Atune.Views
{
    public partial class PlaylistView : UserControl
    {
        public PlaylistView()
        {
            InitializeComponent();
            AddHandler(PointerPressedEvent, OnPointerPressed, handledEventsToo: true);
            AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void GoBack_Click(object? sender, RoutedEventArgs e)
        {
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            mainVm?.GoMediaCommand.Execute(null);
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var kind = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            if (kind == PointerUpdateKind.XButton1Pressed)
            {
                mainVm?.GoMediaCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.MainWindow?.DataContext as MainViewModel;
            if (e.Key == Key.Escape || e.Key == Key.BrowserBack)
            {
                mainVm?.GoMediaCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
} 