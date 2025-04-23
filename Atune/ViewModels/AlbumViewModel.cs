using Atune.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Atune.ViewModels
{
    public class AlbumViewModel : ObservableObject
    {
        public AlbumInfo Album { get; }

        public AlbumViewModel(AlbumInfo album)
        {
            Album = album;
        }
    }
} 
