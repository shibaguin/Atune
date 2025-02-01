using Atune.Models;

namespace Atune.Services;

public interface ISettingsService
{
    void SaveSettings(AppSettings settings);
    AppSettings LoadSettings();
} 