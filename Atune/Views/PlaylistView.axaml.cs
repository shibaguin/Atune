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
        private TextBlock _titleText;
        private TextBox _titleBox;

        public PlaylistView()
        {
            InitializeComponent();
            _titleText = this.FindControl<TextBlock>("TitleText");
            _titleBox = this.FindControl<TextBox>("TitleBox");
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

        // Handler for title TextBlock click: switch to edit mode
        private void OnTitleTextPressed(object? sender, PointerPressedEventArgs e)
        {
            _titleText.IsVisible = false;
            _titleBox.IsVisible = true;
            _titleBox.Focus();
            _titleBox.CaretIndex = _titleBox.Text?.Length ?? 0;
        }

        // Commit rename on Enter key
        private async void OnTitleBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is PlaylistViewModel vm)
            {
                await vm.RenameCommand.ExecuteAsync(null);
                _titleBox.IsVisible = false;
                _titleText.IsVisible = true;
            }
        }

        // Commit rename on losing focus
        private async void OnTitleBoxLostFocus(object? sender, RoutedEventArgs e)
        {
            if (DataContext is PlaylistViewModel vm)
            {
                await vm.RenameCommand.ExecuteAsync(null);
            }
            _titleBox.IsVisible = false;
            _titleText.IsVisible = true;
        }
    }
} 