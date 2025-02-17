using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Atune.ViewModels;
using Atune.Services;
using ThemeVariant = Atune.Models.ThemeVariant;
using Atune.Models;

namespace Atune.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly ISettingsService? _settingsService;

        // Constructor for DI
        public SettingsView(SettingsViewModel viewModel, ISettingsService settingsService)
        {
            InitializeComponent();
            DataContext = viewModel;
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            InitializePlatformSpecificSettings();
        }

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
                
                // For Android, use conditional compilation
#if ANDROID
                var window = TopLevel.GetTopLevel(this) as Window;
                if (window?.PlatformImpl != null)
                {
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

            // Get the view model
            var vm = DataContext as SettingsViewModel;
            if (vm == null)
                return;

            // Convert the selected display name to a language code
            string languageCode = vm.SelectedLanguage switch
            {
                "Русская" => "ru",
                "English" => "en",
                _ => vm.SelectedLanguage
            };

            // Save settings with the selected language and current theme variant
            _settingsService.SaveSettings(new AppSettings
            {
                ThemeVariant = (ThemeVariant)ThemeComboBox.SelectedIndex,
                Language = languageCode
            });

            // Update localization (the UpdateLocalization method must update global resources)
            (Application.Current as App)?.UpdateLocalization();
            
            // Recreate the selected ComboBox item to display new resources
            RefreshSelectedTheme();
        }

        private void ApplySettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_settingsService == null) return;
            
            var vm = (DataContext as SettingsViewModel)!;
            _settingsService.SaveSettings(new AppSettings { 
                ThemeVariant = (ThemeVariant)vm.SelectedThemeIndex 
            });
        }

        private void SaveSettings()
        {
            if (_settingsService == null || ThemeComboBox == null) return;
            
            var vm = (DataContext as SettingsViewModel)!;
            _settingsService.SaveSettings(new AppSettings { 
                ThemeVariant = (ThemeVariant)ThemeComboBox.SelectedIndex 
            });
            ApplyTheme((ThemeVariant)ThemeComboBox.SelectedIndex);
        }

        private void RefreshSelectedTheme()
        {
            // Save the current index
            int currentIndex = ThemeComboBox.SelectedIndex;
            // Reset the selection
            ThemeComboBox.SelectedIndex = -1;
            // Restore the selection, which will trigger a redraw and update the text of the item
            ThemeComboBox.SelectedIndex = currentIndex;
        }
    }
}