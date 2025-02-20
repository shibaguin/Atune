using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Atune.ViewModels;
using Atune.Services;
using ThemeVariant = Atune.Models.ThemeVariant;
using Atune.Models;
using Atune.Utils;

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
            ApplyTheme(settings.ThemeVariant);
            ThemeComboBox.SelectedIndex = (int)settings.ThemeVariant;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveSettings();
        }

        private void ApplyTheme(ThemeVariant theme)
        {
            if (Application.Current is App app)
            {
                app.UpdateTheme(theme);
                
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

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settingsService == null)
                return;

            // Получаем VM
            // Get VM
            var vm = DataContext as SettingsViewModel;
            if (vm == null)
                return;

            // Преобразуем выбранное отображаемое название в код языка для сохранения
            // Convert the selected display name to a language code for saving
            string languageCode = LanguageConverter.DisplayToCode(vm.SelectedLanguage);

            // Сохраняем настройки с текущей темой и выбранным языком
            // Save settings with the current theme and selected language
            _settingsService.SaveSettings(new AppSettings
            {
                ThemeVariant = (ThemeVariant)ThemeComboBox.SelectedIndex,
                Language = languageCode
            });

            // Обновляем локализацию
            (Application.Current as App)?.UpdateLocalization();

            // Перерисовываем выбранный элемент для отображения обновлённых ресурсов
            // Redraw the selected item to display the updated resources
            RefreshSelectedTheme();
        }

        private void ApplySettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_settingsService == null)
                return;
            
            // Получаем VM
            // Get VM
            var vm = DataContext as SettingsViewModel;
            if (vm == null)
                return;
            
            // Преобразуем выбранное отображаемое название в код языка для сохранения
            // Convert the selected display name to a language code for saving
            string languageCode = LanguageConverter.DisplayToCode(vm.SelectedLanguage);

            _settingsService.SaveSettings(new AppSettings 
            { 
                ThemeVariant = (ThemeVariant)vm.SelectedThemeIndex,
                Language = languageCode
            });
        }

        private void SaveSettings()
        {
            if (_settingsService == null || ThemeComboBox == null)
                return;
            
            var vm = DataContext as SettingsViewModel;
            if (vm == null)
                return;
            
            _settingsService.SaveSettings(new AppSettings { 
                ThemeVariant = (ThemeVariant)ThemeComboBox.SelectedIndex,
                Language = LanguageConverter.DisplayToCode(vm.SelectedLanguage)
            });
            
            // Добавляем вызов ApplyTheme
            // Add a call to ApplyTheme
            ApplyTheme((ThemeVariant)ThemeComboBox.SelectedIndex);
        }

        private void RefreshSelectedTheme()
        {
            // Save the current index
            // Сохраняем текущий индекс
            int currentIndex = ThemeComboBox.SelectedIndex;
            // Reset the selection
            // Сбрасываем выбор
            ThemeComboBox.SelectedIndex = -1;
            // Restore the selection, which will trigger a redraw and update the text of the item
            // Восстанавливаем выбор, который вызовет перерисовку и обновление текста элемента
            ThemeComboBox.SelectedIndex = currentIndex;
        }
    }
}