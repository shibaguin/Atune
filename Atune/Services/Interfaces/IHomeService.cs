namespace Atune.Services.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Atune.Models.Dtos;

    public interface IHomeService
    {
        Task<IEnumerable<TopTrackDto>> GetTopTracksAsync(int count = 5);
        Task<IEnumerable<TopAlbumDto>> GetTopAlbumsAsync(int count = 5);
        Task<IEnumerable<TopPlaylistDto>> GetTopPlaylistsAsync(int count = 5);
        Task<IEnumerable<RecentTrackDto>> GetRecentTracksAsync(int count = 5);
    }
}