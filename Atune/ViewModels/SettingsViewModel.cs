using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Atune.Models;
using Atune.Services;
using Atune.Views;
using ThemeVariant = Atune.Models.ThemeVariant;
using System.Collections.Generic;
using System.Timers;

namespace Atune.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private int selectedThemeIndex;
    
    // Default value - displayed language name
    // Значение по умолчанию - отображаемое название языка
    [ObservableProperty]
    private string selectedLanguage = "Русский";

    // Список для выбора: отображаемые названия языков
    // List for selection: displayed language names
    public List<string> AvailableLanguages { get; } = new() { "Русский", "English" };

    private readonly ISettingsService _settingsService;
    private readonly IInterfaceSettingsService _interfaceSettingsService;

    // Новое поле для таймера debounce
    // New field for debounce timer
    private Timer? _autoSaveTimer;

    public SettingsViewModel(ISettingsService settingsService,
                             IInterfaceSettingsService interfaceSettingsService)
    {
        _settingsService = settingsService;
        _interfaceSettingsService = interfaceSettingsService;
        
        // Обновляем настройки интерфейса из файла settings.ini
        // Update interface settings from the settings.ini file
        _interfaceSettingsService.LoadSettings();
        
        // Загружаем общие пользовательские настройки
        // Load general user settings
        var settings = _settingsService.LoadSettings();
        SelectedThemeIndex = (int)settings.ThemeVariant;

        // Преобразуем сохранённый языковой код в отображаемое название
        // Convert saved language code to display name
        SelectedLanguage = Atune.Utils.LanguageConverter.CodeToDisplay(settings.Language);

        // Загружаем настройки интерфейса (из settings.ini) в свойства модели представления
        // Load interface settings (from settings.ini) into view model properties
        HeaderFontSize = _interfaceSettingsService.HeaderFontSize;
        NavigationDividerWidth = _interfaceSettingsService.NavigationDividerWidth;
        NavigationDividerHeight = _interfaceSettingsService.NavigationDividerHeight;
        TopDockHeight = _interfaceSettingsService.TopDockHeight;
        BarHeight = _interfaceSettingsService.BarHeight;
        NavigationFontSize = _interfaceSettingsService.NavigationFontSize;
        BarPadding = _interfaceSettingsService.BarPadding;
    }

    // Новые свойства для настроек интерфейса
    // New properties for interface settings
    [ObservableProperty]
    private double headerFontSize;
    
    [ObservableProperty]
    private double navigationDividerWidth;
    
    [ObservableProperty]
    private double navigationDividerHeight;
    
    [ObservableProperty]
    private double topDockHeight;
    
    [ObservableProperty]
    private double barHeight;
    
    [ObservableProperty]
    private double navigationFontSize;
    
    [ObservableProperty]
    private double barPadding;

    [RelayCommand]
    private void SaveSettings()
    {
        // Преобразуем выбранное отображаемое название в код языка для сохранения
        // Convert selected display name to language code for saving
        string languageCode = Atune.Utils.LanguageConverter.DisplayToCode(SelectedLanguage);

        _settingsService.SaveSettings(new AppSettings
        { 
            ThemeVariant = (ThemeVariant)SelectedThemeIndex,
            Language = languageCode
        });
    }

    // Существующий метод для сохранения настроек интерфейса
    // Existing method for saving interface settings
    private void AutoSaveInterfaceSettings()
    {
        _interfaceSettingsService.UpdateInterfaceSettings(HeaderFontSize,
                                                          NavigationDividerWidth,
                                                          NavigationDividerHeight,
                                                          TopDockHeight,
                                                          BarHeight,
                                                          NavigationFontSize,
                                                          BarPadding);
    }

    // Новый метод для отложенного (debounce) сохранения настроек
    // New method for delayed (debounce) saving settings
    private void ScheduleAutoSave()
    {
        // Если таймер уже запущен, перезапускаем его
        // If the timer is already running, restart it
        if (_autoSaveTimer != null)
        {
            _autoSaveTimer.Stop();
            _autoSaveTimer.Dispose();
            _autoSaveTimer = null;
        }
        _autoSaveTimer = new Timer(500)  // задержка 500 мс / delay 500 ms
        {
            AutoReset = false
        };
        _autoSaveTimer.Elapsed += (sender, args) =>
        {
            AutoSaveInterfaceSettings();
            _autoSaveTimer?.Dispose();
            _autoSaveTimer = null;
        };
        _autoSaveTimer.Start();
    }

    // Изменяем вызовы авто-сохранения в partial-методах:
    // Change auto-save calls in partial methods:
    partial void OnHeaderFontSizeChanged(double oldValue, double newValue) => ScheduleAutoSave();

    partial void OnNavigationDividerWidthChanged(double oldValue, double newValue) => ScheduleAutoSave();

    partial void OnNavigationDividerHeightChanged(double oldValue, double newValue) => ScheduleAutoSave();

    partial void OnTopDockHeightChanged(double oldValue, double newValue) => ScheduleAutoSave();

    partial void OnBarHeightChanged(double oldValue, double newValue) => ScheduleAutoSave();

    partial void OnNavigationFontSizeChanged(double oldValue, double newValue) => ScheduleAutoSave();

    partial void OnBarPaddingChanged(double oldValue, double newValue) => ScheduleAutoSave();

    // Новый метод для восстановления настроек интерфейса до значений по умолчанию
    // New method for restoring interface settings to default values
    [RelayCommand]
    private void RestoreDefaults()
    {
        _interfaceSettingsService.RestoreDefaults();

        // Обновляем свойства модели представления после восстановления настроек
        // Update view model properties after restoring settings
        HeaderFontSize = _interfaceSettingsService.HeaderFontSize;
        NavigationDividerWidth = _interfaceSettingsService.NavigationDividerWidth;
        NavigationDividerHeight = _interfaceSettingsService.NavigationDividerHeight;
        TopDockHeight = _interfaceSettingsService.TopDockHeight;
        BarHeight = _interfaceSettingsService.BarHeight;
        NavigationFontSize = _interfaceSettingsService.NavigationFontSize;
        BarPadding = _interfaceSettingsService.BarPadding;
    }
} 