namespace Atune.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Atune.Services.Interfaces;
    using Atune.Data.Interfaces;
    using Atune.Models.Dtos;

    public class HomeService : IHomeService
    {
        private readonly IHomeRepository _homeRepository;
        public HomeService(IHomeRepository homeRepository)
        {
            _homeRepository = homeRepository;
        }

        public Task<IEnumerable<TopTrackDto>> GetTopTracksAsync(int count = 5) =>
            _homeRepository.GetTopTracksAsync(count);

        public Task<IEnumerable<TopAlbumDto>> GetTopAlbumsAsync(int count = 5) =>
            _homeRepository.GetTopAlbumsAsync(count);

        public Task<IEnumerable<TopPlaylistDto>> GetTopPlaylistsAsync(int count = 5) =>
            _homeRepository.GetTopPlaylistsAsync(count);

        public Task<IEnumerable<RecentTrackDto>> GetRecentTracksAsync(int count = 5) =>
            _homeRepository.GetRecentTracksAsync(count);
    }
} 