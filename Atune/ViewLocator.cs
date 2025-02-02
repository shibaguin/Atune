using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Atune.ViewModels;
using Atune.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Atune;

public class ViewLocator : IDataTemplate
{
    private readonly IServiceProvider _services;
    
    private readonly ViewMapping[] _viewMappings = new[]
    {
        new ViewMapping(typeof(HomeViewModel), typeof(HomeView)),
        new ViewMapping(typeof(MediaViewModel), typeof(MediaView)),
        new ViewMapping(typeof(HistoryViewModel), typeof(HistoryView)),
        new ViewMapping(typeof(SettingsViewModel), typeof(SettingsView))
    };

    public ViewLocator(IServiceProvider services)
    {
        _services = services;
    }

    public Control Build(object? data)
    {
        if (data is null) 
            return new TextBlock { Text = "Null DataContext" };
        
        var viewModelType = data.GetType();
        var mapping = _viewMappings.FirstOrDefault(m => m.ViewModel == viewModelType);
        
        if (mapping.View != null)
        {
            return CreateViewInstance(mapping.View);
        }
        
        return new TextBlock { Text = $"View not found for {viewModelType.Name}" };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "View types are explicitly registered")]
    private Control CreateViewInstance(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type viewType)
    {
        return (Control)ActivatorUtilities.CreateInstance(_services, viewType);
    }

    public bool Match(object? data) => data is ViewModelBase;
}

internal static class ViewLocatorExtensions
{
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public static Type WithDAMembers([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] this Type type)
    {
        return type;
    }
}

internal struct ViewMapping
{
    public Type ViewModel;
    
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type View;

    public ViewMapping(
        Type viewModel, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type view)
    {
        ViewModel = viewModel;
        View = view;
    }
}