using CommunityToolkit.Mvvm.ComponentModel;

namespace Atune.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;
    
    // Добавляем базовую функциональность
}
