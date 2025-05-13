using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Atune.Data.Interfaces;
using Atune.Data.Repositories;
using Atune.Services;
using Microsoft.EntityFrameworkCore;
using Atune.Models;

namespace Atune.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly ILoggerService _logger;
        private IMediaRepository? _mediaRepository;
        private IAlbumRepository? _albumRepository;
        private IArtistRepository? _artistRepository;
        private IPlaylistRepository? _playlistRepository;
        private IPlayHistoryRepository? _playHistoryRepository;

        public UnitOfWork(
            AppDbContext context,
            ILoggerService logger)
        {
            _context = context;
            _logger = logger;
        }

        public IMediaRepository Media => _mediaRepository ??= new MediaRepository(_context);
        public IAlbumRepository Albums => _albumRepository ??= new AlbumRepository(_context);
        public IArtistRepository Artists => _artistRepository ??= new ArtistRepository(_context);
        public IPlaylistRepository Playlists => _playlistRepository ??= new PlaylistRepository(_context);
        public IPlayHistoryRepository PlayHistory => _playHistoryRepository ??= new PlayHistoryRepository(_context);

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error saving changes", ex);
                await RollbackAsync();
                throw;
            }
        }

        public async Task RollbackAsync()
        {
            await _context.Database.RollbackTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
