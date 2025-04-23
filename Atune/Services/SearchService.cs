namespace Atune.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    // Aggregates all registered ISearchProvider implementations
    public interface ISearchService
    {
        Task<IEnumerable<SearchResult>> SearchAllAsync(string query);
    }

    public class SearchService : ISearchService
    {
        private readonly IEnumerable<ISearchProvider> _providers;

        public SearchService(IEnumerable<ISearchProvider> providers)
            => _providers = providers;

        public async Task<IEnumerable<SearchResult>> SearchAllAsync(string query)
        {
            var tasks = _providers.Select(p => p.SearchAsync(query));
            var resultsPerProvider = await Task.WhenAll(tasks);
            return resultsPerProvider.SelectMany(r => r);
        }
    }
} 
