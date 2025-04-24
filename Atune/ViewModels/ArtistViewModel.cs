using Atune.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Atune.ViewModels
{
    public class ArtistViewModel(ArtistInfo artist) : ObservableObject
    {
        public ArtistInfo Artist { get; } = artist;
    }
}
