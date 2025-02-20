using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Atune.Extensions;

public static class ApplicationLifetimeExtensions
{
    public static TopLevel? TryGetTopLevel(this IApplicationLifetime? lifetime)
    {
        return lifetime switch
        {
            // Для настольных приложений
            // For desktop applications
            IClassicDesktopStyleApplicationLifetime desktop => 
                TopLevel.GetTopLevel(desktop.MainWindow),
            
            // Для Android и других платформ с SingleView
            // For Android and other platforms with SingleView
            ISingleViewApplicationLifetime singleView when singleView.MainView is Control mainControl => 
                TopLevel.GetTopLevel(mainControl),
            
            _ => null
        };
    }
} 