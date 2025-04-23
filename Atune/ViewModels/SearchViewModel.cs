namespace Atune.ViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using Atune.Services;
    using System.Threading;
    using System.Collections.Generic;
    using System.Linq;

    public partial class SearchViewModel : ViewModelBase
    {
        private readonly ISearchService _searchService;
        private CancellationTokenSource? _searchCts;
        // Cache for recent search queries (keyed by normalized query)
        private readonly Dictionary<string, List<SearchResult>> _searchCache = new Dictionary<string, List<SearchResult>>();
        private const int MinSearchLength = 2;

        [ObservableProperty]
        private bool isOpen;

        [ObservableProperty]
        private string query = string.Empty;

        partial void OnQueryChanged(string value)
        {
            var q = value?.Trim() ?? string.Empty;
            // Clear results for short or empty queries
            if (q.Length < MinSearchLength)
            {
                Results.Clear();
                return;
            }
            // Debounce search requests
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            _ = DebouncedSearchAsync(q, _searchCts.Token);
        }

        public ObservableCollection<SearchResult> Results { get; } = new ObservableCollection<SearchResult>();

        public IRelayCommand ToggleOpenCommand { get; }
        public IAsyncRelayCommand SearchCommand { get; }

        public SearchViewModel(ISearchService searchService)
        {
            _searchService = searchService;
            ToggleOpenCommand = new RelayCommand(() => IsOpen = !IsOpen);
            SearchCommand = new AsyncRelayCommand(() => OnSearchAsync(query));
        }

        private async Task OnSearchAsync(string q)
        {
            var key = q.ToLowerInvariant();
            Results.Clear();
            // Respect minimum length
            if (key.Length < MinSearchLength)
                return;
            // Return cached results if available
            if (_searchCache.TryGetValue(key, out var cached))
            {
                foreach (var r in cached)
                    Results.Add(r);
                return;
            }
            // Perform search and cache results
            var hits = await _searchService.SearchAllAsync(q);
            var list = hits.ToList();
            _searchCache[key] = list;
            foreach (var r in list)
                Results.Add(r);
        }

        private async Task DebouncedSearchAsync(string q, CancellationToken token)
        {
            try
            {
                // Wait for typing to pause
                await Task.Delay(300, token);
                await OnSearchAsync(q);
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellations
            }
        }
    }
} 