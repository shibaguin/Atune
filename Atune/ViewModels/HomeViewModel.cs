using CommunityToolkit.Mvvm.ComponentModel;

namespace Atune.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    // Реализация ViewModel для главного экрана
    public string WelcomeMessage { get; } = "Добро пожаловать в Atune!";

    public HomeViewModel()
    {
        Title = "Главная страница";
    }
} 