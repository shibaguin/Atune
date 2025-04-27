using System;
using System.Threading.Tasks;
using Atune.Data.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Atune.ViewModels;
using Atune.Views;
using Atune.Services;

namespace Atune.Services
{
    public interface IUtilityService
    {
        Task AddMusicAsync();
        Task AddFolderAsync();
        Task RefreshMediaAsync();
        Task DropMediaRecordsAsync();
        Task PrintDatabaseAsync();
        void ClearQueue();
    }

    public class UtilityService(
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        ILoggerService logger,
        MediaDatabaseService mediaDatabaseService,
        MediaFileService mediaFileService,
        IPlaylistService playlistService,
        ISettingsService settingsService,
        IPlaybackService playbackService) : IUtilityService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMemoryCache _cache = cache;
        private readonly ILoggerService _logger = logger;
        private readonly MediaDatabaseService _mediaDatabaseService = mediaDatabaseService;
        private readonly MediaFileService _mediaFileService = mediaFileService;
        private readonly IPlaylistService _playlistService = playlistService;
        private readonly ISettingsService _settingsService = settingsService;
        private readonly IPlaybackService _playbackService = playbackService;

        private static MediaViewModel? GetMediaViewModel()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return null;
            if (desktop.MainWindow?.DataContext is not MainViewModel mainVm)
                return null;
            // Ensure Media view is active
            mainVm.GoMediaCommand.Execute(null);
            if (mainVm.CurrentView is MediaView mediaView)
                return mediaView.DataContext as MediaViewModel;
            return null;
        }

        public async Task AddMusicAsync()
        {
            var mvm = GetMediaViewModel();
            if (mvm != null)
                await mvm.AddMediaCommand.ExecuteAsync(null);
        }

        public async Task AddFolderAsync()
        {
            var mvm = GetMediaViewModel();
            if (mvm != null)
                await mvm.AddFolderCommand.ExecuteAsync(null);
        }

        public async Task RefreshMediaAsync()
        {
            var mvm = GetMediaViewModel();
            if (mvm != null)
                await mvm.RefreshMediaCommand.ExecuteAsync(null);
        }

        public async Task DropMediaRecordsAsync()
        {
            var mvm = GetMediaViewModel();
            if (mvm != null)
                await mvm.DropMediaRecordsCommand.ExecuteAsync(null);
        }

        public async Task PrintDatabaseAsync()
        {
            var mvm = GetMediaViewModel();
            if (mvm != null)
                await mvm.PrintDatabaseCommand.ExecuteAsync(null);
        }

        public void ClearQueue()
        {
            _playbackService.ClearQueue();
        }
    }
}
