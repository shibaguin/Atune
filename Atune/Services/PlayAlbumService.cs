using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.Models;
using Atune.ViewModels;

namespace Atune.Services
{
    public interface IPlayAlbumService
    {
        Task PlayAlbumAsync(AlbumInfo album);
    }

    public class PlayAlbumService : IPlayAlbumService
    {
        public Task PlayAlbumAsync(AlbumInfo album)
        {
            if (album == null)
                return Task.CompletedTask;

            // Route through MainViewModel to play album
            var mainVm = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                           ?.MainWindow?.DataContext as MainViewModel;
            mainVm?.PlayAlbumCommand.Execute(album);
            return Task.CompletedTask;
        }
    }
} 