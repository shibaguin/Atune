using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Windows.Input;
using Atune.Models;

namespace Atune.Views
{
    public partial class AlbumListView : UserControl
    {
        // OpenCommand for album selection
        public static readonly StyledProperty<ICommand?> OpenCommandProperty =
            AvaloniaProperty.Register<AlbumListView, ICommand?>(nameof(OpenCommand));

        public ICommand? OpenCommand
        {
            get => GetValue(OpenCommandProperty);
            set => SetValue(OpenCommandProperty, value);
        }

        public AlbumListView()
        {
            InitializeComponent();

            var openBtn = this.FindControl<Button>("OpenButton");
            if (openBtn != null)
            {
                openBtn.Click += OpenBtn_Click;
            }
        }

        private void OpenBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (OpenCommand != null)
            {
                OpenCommand.Execute(DataContext as AlbumInfo);
            }
        }
    }
} 