using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Atune.Models;
using Atune.ViewModels;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia;
using System.ComponentModel;
using System;
using Avalonia.Threading;

namespace Atune.Views;


public partial class MainView : UserControl
{
    // Custom progress bar elements
    private Border? _progressBarBackground;
    private Rectangle? _progressBarFill;
    public MainView()
    {
        InitializeComponent();
        // Find custom progress bar elements
        _progressBarBackground = this.FindControl<Border>("ProgressBarBackground");
        _progressBarFill = this.FindControl<Rectangle>("ProgressBarFill");
        if (_progressBarBackground != null)
        {
            _progressBarBackground.PointerPressed += OnProgressBarPointer;
            _progressBarBackground.PointerMoved += OnProgressBarPointer;
        }
        // Subscribe to ViewModel changes
        DataContextChanged += (s, e) =>
        {
            if (DataContext is INotifyPropertyChanged vm)
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;
                // Initial update
                UpdateCustomProgressBar();
            }
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentPosition) ||
            e.PropertyName == nameof(MainViewModel.Duration))
        {
            Dispatcher.UIThread.Post(UpdateCustomProgressBar);
        }
    }

    private void UpdateCustomProgressBar()
    {
        if (_progressBarBackground == null || _progressBarFill == null)
            return;

        if (DataContext is MainViewModel vm && vm.Duration.TotalSeconds > 0)
        {
            var backgroundWidth = _progressBarBackground.Bounds.Width;
            if (backgroundWidth <= 0)
                return;
            double ratio = vm.CurrentPosition.TotalSeconds / vm.Duration.TotalSeconds;
            ratio = Math.Clamp(ratio, 0, 1);
            _progressBarFill.Width = ratio * backgroundWidth;
        }
    }

    private void OnProgressBarPointer(object? sender, PointerEventArgs e)
    {
        if (_progressBarBackground == null || DataContext is not MainViewModel vm)
            return;
        var props = e.GetCurrentPoint(_progressBarBackground).Properties;
        if (props.IsLeftButtonPressed || e is PointerPressedEventArgs)
        {
            var pos = e.GetPosition(_progressBarBackground);
            double ratio = pos.X / _progressBarBackground.Bounds.Width;
            var newTime = TimeSpan.FromSeconds(Math.Clamp(ratio, 0, 1) * vm.Duration.TotalSeconds);
            vm.CurrentPosition = newTime;
        }
    }

    private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SearchCommand.Execute(null);
            }
        }
    }


    private void SearchSuggestions_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is string selectedSuggestion &&
            DataContext is MainViewModel vm)
        {
            listBox.SelectedItem = null;
            vm.SearchQuery = selectedSuggestion;
            vm.IsSuggestionsOpen = false;
        }
    }

    public void PlayButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PlayCommand.Execute(null);
        }
    }

    public void StopButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.StopCommand.Execute(null);
        }
    }

    // XAML event handlers for pointer events
    public void ProgressBarBackground_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        => OnProgressBarPointer(sender, e);
    public void ProgressBarBackground_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        => OnProgressBarPointer(sender, e);
}