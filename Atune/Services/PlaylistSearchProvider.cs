namespace Atune.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class PlaylistSearchProvider : ISearchProvider
    {
        public string Name => "Playlists";

        private readonly IPlaylistService _playlistService;

        public PlaylistSearchProvider(IPlaylistService playlistService)
            => _playlistService = playlistService;

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            var playlists = await _playlistService.GetPlaylistsAsync();
            var matches = playlists
                .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

            foreach (var p in matches)
            {
                results.Add(new SearchResult
                {
                    Title = p.Name,
                    Category = Name,
                    Data = p
                });
            }
            return results;
        }
    }
} 
