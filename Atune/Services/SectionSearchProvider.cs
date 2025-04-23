namespace Atune.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    // Provides search over application sections (Home, Media, History, Settings)
    public class SectionSearchProvider : ISearchProvider
    {
        public string Name => "Navigation";

        private static readonly string[] Sections = new[] { "Home", "Media", "History", "Settings" };

        public Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();
            foreach (var section in Sections)
            {
                if (section.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new SearchResult
                    {
                        Title = section,
                        Category = Name,
                        Data = section
                    });
                }
            }
            return Task.FromResult<IEnumerable<SearchResult>>(results);
        }
    }
} 
