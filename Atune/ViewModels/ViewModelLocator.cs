// ViewModelLocator.cs
using Atune.ViewModels;

namespace Atune.ViewModels;

public class ViewModelLocator
{
    public static MainViewModel MainViewModel { get; } = new MainViewModel();
}