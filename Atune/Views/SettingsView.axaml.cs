using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Atune.ViewModels;
using Atune.Services;
using Atune.Models;
using ThemeVariant = Atune.Models.ThemeVariant;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace Atune.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContext = ServiceLocator.GetService<SettingsViewModel>();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = SettingsManager.LoadSettings();
            ApplyTheme(settings.ThemeVariant);
            ThemeComboBox.SelectedIndex = (int)settings.ThemeVariant;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox == null) return;
            
            var settings = SettingsManager.LoadSettings();
            settings.ThemeVariant = (ThemeVariant)ThemeComboBox.SelectedIndex;
            SettingsManager.SaveSettings(settings);
            ApplyTheme(settings.ThemeVariant);
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
    }
}