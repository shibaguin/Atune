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

        private readonly MediaDatabaseService _dbService;

        public ArtistSearchProvider(MediaDatabaseService dbService)
            => _dbService = dbService;

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();
            var q = query?.Trim() ?? string.Empty;
            if (q.Length < 2)
                return results;

            // Prepare fallback variants for slash/hyphen
            var qHyphen = q.Replace('/', '-');
            var qSlash = q.Replace('-', '/');
            // Load all media items with details
            var allItems = await _dbService.GetAllMediaItemsAsync();
            // Filter tracks by artist name matching query or fallback
            var matching = allItems
                .Where(m => m.TrackArtists
                    .Any(ta => ta.Artist.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                              || ta.Artist.Name.Contains(qHyphen, StringComparison.OrdinalIgnoreCase)
                              || ta.Artist.Name.Contains(qSlash, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            // Group by artist name and matching variant
            var groups = matching
                .SelectMany(m => m.TrackArtists
                    .Where(ta => ta.Artist.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                               || ta.Artist.Name.Contains(qHyphen, StringComparison.OrdinalIgnoreCase)
                               || ta.Artist.Name.Contains(qSlash, StringComparison.OrdinalIgnoreCase))
                    .Select(ta => new { m, ArtistName = ta.Artist.Name }))
                .GroupBy(x => x.ArtistName, StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
            {
                var artistName = group.Key;
                var tracks = group.Select(x => x.m).ToList();
                var artistInfo = new ArtistInfo(artistName, tracks);
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