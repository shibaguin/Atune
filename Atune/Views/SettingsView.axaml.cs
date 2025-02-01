using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Atune.ViewModels;
using Atune.Services;
using Atune.Models;
using ThemeVariant = Atune.Models.ThemeVariant;

namespace Atune.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
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
            
            var settings = new AppSettings { ThemeVariant = (ThemeVariant)ThemeComboBox.SelectedIndex };
            SettingsManager.SaveSettings(settings);
            ApplyTheme(settings.ThemeVariant);
        }

        private void ApplyTheme(ThemeVariant theme)
        {
            var app = Application.Current as App;
            if (app != null)
            {
                app.RequestedThemeVariant = theme switch
                {
                    ThemeVariant.Light => Avalonia.Styling.ThemeVariant.Light,
                    ThemeVariant.Dark => Avalonia.Styling.ThemeVariant.Dark,
                    _ => Avalonia.Styling.ThemeVariant.Default
                };
            }
        }
    }
}