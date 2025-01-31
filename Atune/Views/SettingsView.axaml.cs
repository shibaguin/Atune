using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Atune.ViewModels;

namespace Atune.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            ApplyTheme(ThemeVariant.System);
        }
        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox == null) return;
            if (ThemeComboBox.SelectedIndex == 0)
            {
                // Системная тема
                ApplyTheme(ThemeVariant.System);
            }
            else if (ThemeComboBox.SelectedIndex == 1)
            {
                // Светлая тема
                ApplyTheme(ThemeVariant.Light);
            }
            else if (ThemeComboBox.SelectedIndex == 2)
            {
                // Тёмная тема
                ApplyTheme(ThemeVariant.Dark);
            }
        }

        private void ApplyTheme(Atune.Views.ThemeVariant theme)
        {
            // Устанавливаем выбранную тему
            var app = Application.Current as App;
            if (app != null)
            {
                switch (theme)
                {
                    case Atune.Views.ThemeVariant.Light:
                        app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
                        break;
                    case Atune.Views.ThemeVariant.Dark:
                        app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                        break;
                    default:
                        // Если выбрана системная тема, используем текущую системную тему
                        app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
                        break;
                }
            }
        }
    }

    // Перечисление для удобства работы с темами
    public enum ThemeVariant
    {
        System,
        Light,
        Dark
    }
}