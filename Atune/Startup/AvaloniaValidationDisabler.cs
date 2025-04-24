using System.Linq;
using Avalonia.Data.Core.Plugins;

namespace Atune.Startup
{
    public static class AvaloniaValidationDisabler
    {
        public static void Disable()
        {
            var plugins = BindingPlugins.DataValidators
                .OfType<DataAnnotationsValidationPlugin>()
                .ToArray();

            foreach (var plugin in plugins)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}