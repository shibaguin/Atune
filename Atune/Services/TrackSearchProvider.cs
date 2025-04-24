namespace Atune.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class TrackSearchProvider(MediaDatabaseService dbService) : ISearchProvider
    {
        public string Name => "Tracks";

        private readonly MediaDatabaseService _dbService = dbService;

        public async Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            var items = await _dbService.GetAllMediaItemsAsync();
            var matches = items
                .Where(m => !string.IsNullOrEmpty(m.Title) && m.Title.Contains(query, StringComparison.OrdinalIgnoreCase));

            foreach (var item in matches)
            {
                results.Add(new SearchResult
                {
                    Title = item.Title,
                    Category = Name,
                    Data = item
                });
            }
            return results;
        }
    }
}
