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

        public UnitOfWork(
            AppDbContext context,
            ILoggerService logger)
        {
            _context = context;
            _logger = logger;
            Media = new MediaRepository(_context);
            PlayHistory = new PlayHistoryRepository(_context);
        }

        public IMediaRepository Media { get; }
        public IPlayHistoryRepository PlayHistory { get; }

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
