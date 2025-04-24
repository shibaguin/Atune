namespace Atune.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows.Input;

    // Represents a single search hit within the application
    public class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public object? Data { get; set; }
        public ICommand? NavigateCommand { get; set; }
    }

    // Contract for pluggable providers that search a specific domain (Settings, Albums, Tracks, etc.)
    public interface ISearchProvider
    {
        // Display name of this provider (shown as grouping or label)
        string Name { get; }

        // Return a list of SearchResult for the given query
        Task<IEnumerable<SearchResult>> SearchAsync(string query);
    }
}
