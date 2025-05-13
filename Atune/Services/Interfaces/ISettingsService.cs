using Atune.Models;
using System.Threading.Tasks;

namespace Atune.Services;

public interface ISettingsService
{
    int Volume { get; set; }
    Task SaveSettingsAsync(AppSettings settings);
    Task<AppSettings> LoadSettingsAsync();
    void SaveSettings(AppSettings settings);
    AppSettings LoadSettings();
    WindowSettings GetWindowSettings();
    Task SaveWindowSettingsAsync(WindowSettings settings);
}
