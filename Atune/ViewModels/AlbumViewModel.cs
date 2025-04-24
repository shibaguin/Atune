using Atune.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Atune.ViewModels
{
    public class AlbumViewModel(AlbumInfo album) : ObservableObject
    {
        public AlbumInfo Album { get; } = album;
    }
}
