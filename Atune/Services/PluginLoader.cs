using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Atune.Plugins.Abstractions;
using Atune.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Runtime.Loader;
using System.Composition.Convention;
using System.Composition.Hosting.Core;
using System.Composition;

public class PluginLoader
{
    private readonly IPlatformPathService _pathService;
    private readonly ILoggerService _logger;
    private readonly List<Assembly> _loadedAssemblies = new();

    public PluginLoader(IPlatformPathService pathService, ILoggerService logger)
    {
        _pathService = pathService;
        _logger = logger;
    }

    public IEnumerable<Lazy<IPlugin, IPluginMetadata>> LoadPlugins()
    {
        var pluginsDir = _pathService.GetPluginsDirectory();
        if (!Directory.Exists(pluginsDir)) return Enumerable.Empty<Lazy<IPlugin, IPluginMetadata>>();

        var configuration = new ContainerConfiguration();
        
        foreach (var pluginDir in Directory.GetDirectories(pluginsDir))
        {
            try
            {
                var pluginInfo = LoadPluginManifest(pluginDir);
                if (pluginInfo == null || !ValidateDependencies(pluginInfo)) continue;

                var assembly = LoadPluginAssembly(pluginDir);
                if (assembly == null) continue;

                configuration.WithAssembly(assembly);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load plugin from {pluginDir}: {ex.Message}");
            }
        }

        var container = configuration.CreateContainer();
        var exportFactories = container.GetExports<ExportFactory<IPlugin, IPluginMetadata>>();
        return exportFactories.ToLazy();
    }

    private PluginManifest? LoadPluginManifest(string pluginDir)
    {
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("Plugin manifest not found");

        return JsonConvert.DeserializeObject<PluginManifest>(File.ReadAllText(manifestPath));
    }

    private bool ValidateDependencies(PluginManifest manifest)
    {
        // Реализуйте проверку версий зависимостей
        return true;
    }

    private Assembly? LoadPluginAssembly(string pluginDir)
    {
#if ANDROID
        return LoadAndroidAssembly(pluginDir);
#else
        return LoadDesktopAssembly(pluginDir);
#endif
    }

    private Assembly LoadDesktopAssembly(string pluginDir)
    {
        var assemblyFile = Directory.GetFiles(pluginDir, "*.dll")
            .FirstOrDefault(f => !f.EndsWith(".Views.dll") && !f.EndsWith(".ViewModels.dll"));
        
        if (assemblyFile == null) return null!;
        
        var resolver = new AssemblyDependencyResolver(assemblyFile);
        var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(assemblyFile), true);
        
        context.Resolving += (loadContext, name) => {
            var path = resolver.ResolveAssemblyToPath(name);
            return path != null ? loadContext.LoadFromAssemblyPath(path) : null;
        };
        
        return context.LoadFromAssemblyPath(assemblyFile);
    }

    private Assembly LoadAndroidAssembly(string pluginDir)
    {
        try
        {
            var assemblyFile = Path.Combine(pluginDir, $"{new DirectoryInfo(pluginDir).Name}.dll");
            var resolver = new AssemblyDependencyResolver(assemblyFile);
            
            using var stream = File.OpenRead(assemblyFile);
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            var assembly = Assembly.Load(memoryStream.ToArray());
            _loadedAssemblies.Add(assembly);
            
            return assembly;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Android assembly load failed: {ex.Message}");
            return null!;
        }
    }
}

public class PluginManifest
{
    [JsonProperty("id")] public string? Id { get; set; }
    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("version")] public string? Version { get; set; }
    [JsonProperty("dependencies")] public Dictionary<string, string>? Dependencies { get; set; }
} 