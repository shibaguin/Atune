using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Atune.Extensions;

public static class ApplicationLifetimeExtensions
{
    public static TopLevel? TryGetTopLevel(this IApplicationLifetime? lifetime)
    {
        return lifetime switch
        {
            // Для десктопных приложений
            IClassicDesktopStyleApplicationLifetime desktop => 
                TopLevel.GetTopLevel(desktop.MainWindow),
            
            // Для Android и других платформ с SingleView
            ISingleViewApplicationLifetime singleView when singleView.MainView is Control mainControl => 
                TopLevel.GetTopLevel(mainControl),
            
            _ => null
        };
    }
} 