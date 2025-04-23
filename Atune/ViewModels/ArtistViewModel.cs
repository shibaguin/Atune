using Atune.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Atune.ViewModels
{
    public class ArtistViewModel : ObservableObject
    {
        public ArtistInfo Artist { get; }

        public ArtistViewModel(ArtistInfo artist)
        {
            Artist = artist;
        }
    }
} 