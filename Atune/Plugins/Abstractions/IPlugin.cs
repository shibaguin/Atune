using System.Composition;

namespace Atune.Plugins.Abstractions;

public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    void Initialize();
    void Shutdown();
}

public interface IAudioVisualizerPlugin : IPlugin
{
    object CreateVisualizer();
}

public interface IPluginMetadata
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
}

public interface IThemePlugin : IPlugin, IPluginMetadata
{
    string ThemeId { get; }
    string DisplayName { get; }
    string? Description { get; }
    string? PreviewImagePath { get; }
    object GetResourceDictionary(); // object для совместимости с Avalonia и загрузкой XAML
}
