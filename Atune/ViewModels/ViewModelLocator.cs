using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using System;
using Avalonia.Controls.Templates;
using System.Linq;

namespace Atune.ViewModels;

public class ViewModelLocator
{
    public static bool GetAutoHookedUpViewModel(Control obj)
    {
        return obj.GetValue(AutoHookedUpViewModelProperty);
    }

    public static void SetAutoHookedUpViewModel(Control obj, bool value)
    {
        obj.SetValue(AutoHookedUpViewModelProperty, value);
    }

    public static readonly AttachedProperty<bool> AutoHookedUpViewModelProperty =
        AvaloniaProperty.RegisterAttached<ViewModelLocator, Control, bool>(
            "AutoHookedUpViewModel", 
            defaultValue: false);

    static ViewModelLocator()
    {
        AutoHookedUpViewModelProperty.Changed.Subscribe(
            new ActionObserver<AvaloniaPropertyChangedEventArgs<bool>>(OnAutoHookedUpViewModelChanged));
    }

    private static void OnAutoHookedUpViewModelChanged(AvaloniaPropertyChangedEventArgs<bool> e)
    {
        if (e.Sender is Control control && 
            control.DataContext == null && 
            e.NewValue.Value &&
            App.Current?.Services is IServiceProvider services)
        {
            var viewType = control.GetType();
            var viewModelTypeName = viewType.FullName?
                .Replace("Views", "ViewModels")
                .Replace("View", "ViewModel");

            if (string.IsNullOrEmpty(viewModelTypeName)) return;
            
            var viewModelType = Type.GetType(viewModelTypeName) 
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == viewModelTypeName);

            if (viewModelType != null)
            {
                control.DataContext = services.GetService(viewModelType);
            }
        }
    }
}

internal class ActionObserver<T> : IObserver<T>
{
    private readonly Action<T> _onNext;
    public ActionObserver(Action<T> onNext) => _onNext = onNext;
    public void OnNext(T value) => _onNext(value);
    public void OnError(Exception error) { }
    public void OnCompleted() { }
}