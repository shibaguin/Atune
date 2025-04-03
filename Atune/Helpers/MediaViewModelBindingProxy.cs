using Atune.ViewModels;
using Avalonia;

namespace Atune.Helpers
{
    public class MediaViewModelBindingProxy : AvaloniaObject
    {
        // Свойство Data теперь имеет тип MediaViewModel
        public static readonly StyledProperty<MediaViewModel> DataProperty =
            AvaloniaProperty.Register<MediaViewModelBindingProxy, MediaViewModel>(nameof(Data));

        public MediaViewModel Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
} 