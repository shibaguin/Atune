using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Atune.Extensions;

public static class ApplicationLifetimeExtensions
{
    public static TopLevel? TryGetTopLevel(this IApplicationLifetime? lifetime)
    {
        return lifetime switch
        {
            // For desktop applications
            IClassicDesktopStyleApplicationLifetime desktop => 
                TopLevel.GetTopLevel(desktop.MainWindow),
            
            // For Android and other platforms with SingleView
            ISingleViewApplicationLifetime singleView when singleView.MainView is Control mainControl => 
                TopLevel.GetTopLevel(mainControl),
            
            _ => null
        };
    }
} 