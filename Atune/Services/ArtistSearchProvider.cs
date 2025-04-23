namespace Atune.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Atune.Data.Repositories;
    using Atune.Models;

    // Provides search over artist names
    public class ArtistSearchProvider : ISearchProvider
    {
        public string Name => "Artists";

        private readonly IArtistRepository _artistRepository;

        public ArtistSearchProvider(IArtistRepository artistRepository) => _artistRepository = artistRepository;

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            var artists = await _artistRepository.SearchArtistsAsync(query);
            foreach (var artist in artists)
            {
                // Fetch tracks for artist
                var tracks = (await _artistRepository.GetSongsForArtistAsync(artist.Id)).ToList();
                var artistInfo = new ArtistInfo(artist.Name, tracks);
                results.Add(new SearchResult
                {
                    Title = artistInfo.ArtistName,
                    Category = Name,
                    Data = artistInfo
                });
            }
            return results;
        }
    }
} 