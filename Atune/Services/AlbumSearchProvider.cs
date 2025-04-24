namespace Atune.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Atune.Models;

    // Provides search over album titles
    public class AlbumSearchProvider(MediaDatabaseService dbService) : ISearchProvider
    {
        public string Name => "Albums";

        private readonly MediaDatabaseService _dbService = dbService;

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            // Fetch all media items and group matching ones by album title
            var items = await _dbService.GetAllMediaItemsAsync();
            var matching = items
                .Where(m => m.Album != null && m.Album.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
            var groups = matching.GroupBy(m => m.Album.Title);
            foreach (var group in groups)
            {
                var first = group.First();
                // Construct AlbumInfo for navigation including tracks
                var artistName = first.TrackArtists.FirstOrDefault()?.Artist.Name ?? string.Empty;
                // Use track metadata year (first track's Year) instead of DB album Year
                var metadataYear = first.Year;
                var albumInfo = new AlbumInfo(
                    first.Album.Title,
                    artistName,
                    metadataYear,
                    [.. group]);
                results.Add(new SearchResult
                {
                    Title = albumInfo.AlbumName,
                    Category = Name,
                    Data = albumInfo
                });
            }
            return results;
        }
    }
}
