using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Atune.ViewModels;
using Atune.Services;
using Atune.Models;
using Atune.Utils;
using Atune.Plugins.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Atune.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly ISettingsService? _settingsService;

        // Конструктор для DI
        // Constructor for DI
        public SettingsView(SettingsViewModel viewModel, ISettingsService settingsService)
        {
            InitializeComponent();
            DataContext = viewModel;
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            InitializePlatformSpecificSettings();
            LoadThemes();
        }

        // Конструктор для XAML (только для дизайнера)
        // Constructor for XAML (only for designer)
        public SettingsView()
        {
            if (!Design.IsDesignMode)
                throw new InvalidOperationException("Constructor for design mode only!");

            InitializeComponent();
        }

        private void InitializePlatformSpecificSettings()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (_settingsService == null) return;

            var settings = _settingsService.LoadSettings();
            ApplyTheme(settings.ThemeId);
        }

        private void LoadThemes()
        {
            if (DataContext is not SettingsViewModel vm) return;

            // Получаем локализованные строки через LocalizationService
            var loc = (App.Current?.Services?.GetService(typeof(LocalizationService))) as LocalizationService;
            string sys = loc != null ? loc["Theme_System"] : "System";
            string light = loc != null ? loc["Theme_Light"] : "Light";
            string dark = loc != null ? loc["Theme_Dark"] : "Dark";

            UpdateOrAddTheme(vm, "System", sys);
            UpdateOrAddTheme(vm, "Light", light);
            UpdateOrAddTheme(vm, "Dark", dark);

            // Темы-плагины
            var pluginLoader = (App.Current?.Services?.GetService(typeof(PluginLoader))) as PluginLoader;
            var validIds = new List<string> { "System", "Light", "Dark" };
            if (pluginLoader != null)
            {
                foreach (var theme in pluginLoader.GetRegisteredThemePlugins())
                {
                    string displayName = theme.DisplayName;
                    if (loc != null && !string.IsNullOrWhiteSpace(displayName) && loc[displayName] != null && !displayName.StartsWith(" "))
                        displayName = loc[displayName];
                    UpdateOrAddTheme(vm, theme.ThemeId, displayName, theme.Description, theme.PreviewImagePath);
                    validIds.Add(theme.ThemeId);
                }
            }

            // Удаляем лишние темы
            for (int i = vm.ThemeItems.Count - 1; i >= 0; i--)
                if (!validIds.Contains(vm.ThemeItems[i].Id))
                    vm.ThemeItems.RemoveAt(i);

            // Выставляем выбранную тему
            vm.SelectedThemeItem = vm.ThemeItems.FirstOrDefault(x => x.Id == vm.SelectedThemeId);
        }

        private void UpdateOrAddTheme(SettingsViewModel vm, string id, string displayName, string? description = null, string? preview = null)
        {
            var theme = vm.ThemeItems.FirstOrDefault(x => x.Id == id);
            if (theme != null)
            {
                theme.DisplayName = displayName;
                theme.Description = description;
                theme.PreviewImagePath = preview;
            }
            else
            {
                vm.ThemeItems.Add(new ThemeItem { Id = id, DisplayName = displayName, Description = description, PreviewImagePath = preview });
            }
        }

        private string GetLocalizedThemeName(string key)
        {
            // Если есть LocalizationService — используйте его
            var loc = (App.Current?.Services?.GetService(typeof(LocalizationService))) as LocalizationService;
            if (loc != null)
                return loc[key];
            // Fallback: Properties.Resources.ResourceManager.GetString(key)
            return key;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not SettingsViewModel vm)
                return;

            // Сохраняем и применяем выбранную тему
            _settingsService?.SaveSettings(new AppSettings
            {
                ThemeId = vm.SelectedThemeId,
                Language = LanguageConverter.DisplayToCode(vm.SelectedLanguage)
            });
            ApplyTheme(vm.SelectedThemeId);
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settingsService == null)
                return;

            if (DataContext is not SettingsViewModel vm)
                return;

            string languageCode = LanguageConverter.DisplayToCode(vm.SelectedLanguage);

            _settingsService.SaveSettings(new AppSettings
            {
                ThemeId = vm.SelectedThemeId,
                Language = languageCode
            });

            (Application.Current as App)?.UpdateLocalization();

            // После смены языка пересоздаём темы с новыми локализациями
            LoadThemes();
        }

        private void ApplySettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_settingsService == null)
                return;

            // Получаем VM
            // Get VM
            if (DataContext is not SettingsViewModel vm)
                return;

            // Преобразуем выбранное отображаемое название в код языка для сохранения
            // Convert the selected display name to a language code for saving
            string languageCode = LanguageConverter.DisplayToCode(vm.SelectedLanguage);

            _settingsService.SaveSettings(new AppSettings
            {
                ThemeId = ThemeComboBox.SelectedValue?.ToString() ?? "System",
                Language = languageCode
            });
        }

        private static void ApplyTheme(string themeId)
        {
            if (Application.Current is App app)
            {
                app.UpdateTheme(themeId);

                // Для Android, используйте условное компиляцию
                // For Android, use conditional compilation
#if ANDROID
                var window = TopLevel.GetTopLevel(this) as Window;
                if (window?.PlatformImpl != null)
                {
                    // Используйте правильный метод для Android
                    // Use the correct method for Android
                    (window.PlatformImpl as Avalonia.Android.AndroidWindow)?.UpdateSystemTheme();
                }
#endif
            }
        }
    }
}
