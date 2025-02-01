// ViewModelLocator.cs
using Atune.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Atune.ViewModels;

public class ViewModelLocator
{
    private readonly IServiceProvider _provider;

    public ViewModelLocator(IServiceProvider provider)
    {
        _provider = provider;
    }

    public MainViewModel MainViewModel => _provider.GetRequiredService<MainViewModel>();
}