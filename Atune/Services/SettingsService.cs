using Atune.Models;

namespace Atune.Services;

public class SettingsService : ISettingsService
{
    public AppSettings LoadSettings() => SettingsManager.LoadSettings();
    public void SaveSettings(AppSettings settings) => SettingsManager.SaveSettings(settings);
} 