using System;
using Microsoft.Extensions.DependencyInjection;

namespace Atune;

public static class ServiceLocator
{
    private static IServiceProvider? _provider;
    
    public static void Initialize(IServiceProvider provider)
    {
        _provider = provider;
    }
    
    public static T GetService<T>() where T : class 
    {
        return _provider?.GetService<T>() 
            ?? throw new InvalidOperationException("Service provider not initialized");
    }
} 