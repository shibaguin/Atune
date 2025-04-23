using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using System;
using Avalonia.Controls.Templates;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

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

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "View models are explicitly registered")]
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
                    .SelectMany(a => GetAssemblyTypes(a))
                    .FirstOrDefault(t => t.FullName == viewModelTypeName);

            if (viewModelType != null)
            {
                control.DataContext = services.GetService(viewModelType);
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Using GetTypes is required for design-time")]
    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Type name is constructed from known view types")]
    private static IEnumerable<Type> GetAssemblyTypes(System.Reflection.Assembly assembly)
    {
        try 
        {
            return assembly.GetTypes();
        }
        catch
        {
            return Array.Empty<Type>();
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
