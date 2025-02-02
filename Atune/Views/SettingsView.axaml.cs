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

        // Конструктор для DI
        public SettingsView(SettingsViewModel viewModel, ISettingsService settingsService)
        {
            InitializeComponent();
            DataContext = viewModel;
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            InitializePlatformSpecificSettings();
        }

        // Конструктор для XAML (только для дизайнера)
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
            if (_settingsService == null || ThemeComboBox == null) return;
            
            var vm = (DataContext as SettingsViewModel)!;
            _settingsService.SaveSettings(new AppSettings { 
                ThemeVariant = (ThemeVariant)ThemeComboBox.SelectedIndex 
            });
            ApplyTheme((ThemeVariant)ThemeComboBox.SelectedIndex);
        }

        private void ApplyTheme(ThemeVariant theme)
        {
            if (Application.Current is App app)
            {
                app.UpdateTheme(theme);
                
                // Для Android используем условную компиляцию
#if ANDROID
                var window = TopLevel.GetTopLevel(this) as Window;
                if (window?.PlatformImpl != null)
                {
                    // Используем правильный метод для Android
                    (window.PlatformImpl as Avalonia.Android.AndroidWindow)?.UpdateSystemTheme();
                }
#endif
            }
        }

        private void ApplySettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_settingsService == null) return;
            
            var vm = (DataContext as SettingsViewModel)!;
            _settingsService.SaveSettings(new AppSettings { 
                ThemeVariant = (ThemeVariant)vm.SelectedThemeIndex 
            });
        }
    }
}