using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Atune.ViewModels;
using Atune.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Atune;

public class ViewLocator : IDataTemplate
{
    private readonly IServiceProvider _services;
    private readonly Dictionary<Type, Type> _viewMapping = new()
    {
        { typeof(HomeViewModel), typeof(HomeView) },
        { typeof(MediaViewModel), typeof(MediaView) },
        { typeof(HistoryViewModel), typeof(HistoryView) },
        { typeof(SettingsViewModel), typeof(SettingsView) }
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
        if (_viewMapping.TryGetValue(viewModelType, out var viewType))
        {
            return (Control)ActivatorUtilities.CreateInstance(_services, viewType);
        }
        
        return new TextBlock { Text = $"View not found for {viewModelType.Name}" };
    }

    public bool Match(object? data) => data is ViewModelBase;
}