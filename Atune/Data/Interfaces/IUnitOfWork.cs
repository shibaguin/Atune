using Atune.Data.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

public interface IUnitOfWork : IDisposable
{
    IMediaRepository Media { get; }
    IAlbumRepository Albums { get; }
    IArtistRepository Artists { get; }
    IPlaylistRepository Playlists { get; }
    IPlayHistoryRepository PlayHistory { get; }
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync();
}
