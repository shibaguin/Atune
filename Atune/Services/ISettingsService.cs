using Atune.Models;
using System.Threading.Tasks;

namespace Atune.Services;

public interface ISettingsService
{
    Task SaveSettingsAsync(AppSettings settings);
    Task<AppSettings> LoadSettingsAsync();
    void SaveSettings(AppSettings settings);
    AppSettings LoadSettings();
} 