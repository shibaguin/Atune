using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Atune.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Atune;

public class ViewLocator : IDataTemplate
{
    private readonly IServiceProvider _services;

    public ViewLocator(IServiceProvider services)
    {
        _services = services;
    }

    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        return type != null 
            ? (Control)ActivatorUtilities.CreateInstance(_services, type)
            : new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data) => data is ViewModelBase;
}