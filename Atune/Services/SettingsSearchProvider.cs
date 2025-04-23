namespace Atune.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class SettingsSearchProvider : ISearchProvider
    {
        public string Name => "Settings";

        private static readonly string[] Themes = { "System", "Light", "Dark" };
        private static readonly string[] Languages = { "Русский", "English" };

        public Task<IEnumerable<SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchResult>();
            if (string.IsNullOrWhiteSpace(query))
                return Task.FromResult<IEnumerable<SearchResult>>(results);

            var q = query.Trim();
            foreach (var t in Themes.Where(x => x.Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(new SearchResult
                {
                    Category = Name,
                    Title = $"Theme: {t}",
                    Data = t
                });
            }
            foreach (var l in Languages.Where(x => x.Contains(q, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(new SearchResult
                {
                    Category = Name,
                    Title = $"Language: {l}",
                    Data = l
                });
            }
            return Task.FromResult<IEnumerable<SearchResult>>(results);
        }
    }
} 