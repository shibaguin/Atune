using System;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Atune.Views;
using Atune.Services;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Atune.Exceptions;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.IO;
using LibVLCSharp.Shared;
using Atune.Models;
using Atune.ViewModels;
using System.Threading;
using Avalonia.Media;
using Atune.Converters;
namespace Atune.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    public enum SectionType { Home, Media, History, Settings }

    [ObservableProperty]
    private SectionType _selectedSection;

    [ObservableProperty]
    private string _headerText = "Atune";

    [ObservableProperty]
    private Control? _currentView;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private string searchMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> searchSuggestions = [];

    [ObservableProperty]
    private bool isSuggestionsOpen;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private int volume = 50;

    [ObservableProperty]
    private string currentMediaPath = string.Empty;

    [ObservableProperty]
    private TimeSpan _currentPosition;

    [ObservableProperty]
    private TimeSpan _duration;

    [ObservableProperty]
    private MediaItem? _currentMediaItem;

    public string NowPlayingTitle => CurrentMediaItem?.Title ?? string.Empty;
    public string NowPlayingArtist => CurrentMediaItem?.TrackArtists.FirstOrDefault()?.Artist?.Name ?? string.Empty;

    partial void OnCurrentMediaItemChanged(MediaItem? value)
    {
        OnPropertyChanged(nameof(NowPlayingTitle));
        OnPropertyChanged(nameof(NowPlayingArtist));
    }

    private CancellationTokenSource? _volumeSaveCts;
    private bool _isInitializing = true; // skip saves during initial load

    private readonly ISettingsService _settingsService;
    private readonly Dictionary<SectionType, Control> _views;
    private readonly Func<Type, ViewModelBase> _viewModelFactory;
    private readonly Func<Type, Control> _viewFactory;
    private readonly INavigationKeywordProvider _keywordProvider;
    private readonly LocalizationService _localizationService;
    private readonly IPlaybackService _playbackService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly ICoverArtService _coverArtService;
    private readonly SearchViewModel _searchViewModel;
    public SearchViewModel Search => _searchViewModel;

    // Expose the MediaViewModel instance for saving and restoring playback state
    public MediaViewModel? MediaViewModelInstance => _views.TryGetValue(SectionType.Media, out var view) ? view.DataContext as MediaViewModel : null;

    // ????????? ? ?????????? ??????? ??? ??????
    private const string PlayIconPath = "M5 5.274c0-1.707 1.826-2.792 3.325-1.977l12.362 6.726c1.566.853 1.566 3.101 0 3.953L8.325 20.702C6.826 21.518 5 20.432 5 18.726V5.274Z";

    private const string PauseIconPath = "M5.746 3a1.75 1.75 0 0 0-1.75 1.75v14.5c0 .966.784 1.75 1.75 1.75h3.5a1.75 1.75 0 0 0 1.75-1.75V4.75A1.75 1.75 0 0 0 9.246 3h-3.5ZM14.746 3a1.75 1.75 0 0 0-1.75 1.75v14.5c0 .966.784 1.75 1.75 1.75h3.5a1.75 1.75 0 0 0 1.75-1.75V4.75A1.75 1.75 0 0 0 18.246 3h-3.5Z";

    // ??????????? ???????? ??? ???????? ? XAML
    public string PlayIconData => IsPlaying ? PauseIconPath : PlayIconPath;

    // Add at class level
    private DateTime _lastToggleTime = DateTime.MinValue;

    public MainViewModel(
        ISettingsService settingsService,
        Func<Type, ViewModelBase> viewModelFactory,
        Func<Type, Control> viewFactory,
        INavigationKeywordProvider keywordProvider,
        LocalizationService localizationService,
        IPlaybackService playbackService,
        ILogger<MainViewModel> logger,
        ICoverArtService coverArtService,
        SearchViewModel searchViewModel)
    {
        _searchViewModel = searchViewModel;
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        _viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        _keywordProvider = keywordProvider ?? throw new ArgumentNullException(nameof(keywordProvider));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _coverArtService = coverArtService ?? throw new ArgumentNullException(nameof(coverArtService));

        _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));
        LoadInitialSettings();
        _isInitializing = false;

        _views = new Dictionary<SectionType, Control>
        {
            [SectionType.Home] = _viewFactory(typeof(HomeView)),
            [SectionType.Media] = _viewFactory(typeof(MediaView)),
            [SectionType.History] = _viewFactory(typeof(HistoryView)),
            [SectionType.Settings] = _viewFactory(typeof(SettingsView))
        };

        CurrentView = _views[SectionType.Home];
        HeaderText = _localizationService["Nav_Home"];

        // Subscribe to localization change event.
        _localizationService.PropertyChanged += LocalizationService_PropertyChanged;

        // Subscribe to playback service events
        _playbackService.TrackChanged += (_, item) =>
        {
            CurrentMediaItem = item;
            // Update total duration when track changes
            Duration = _playbackService.Duration;
        };
        _playbackService.PlaybackStateChanged += (_, playing) => IsPlaying = playing;
        _playbackService.PositionChanged += (_, pos) =>
        {
            CurrentPosition = pos;
            // Refresh duration in case metadata loaded or track changed
            Duration = _playbackService.Duration;
        };
    }

    private void LocalizationService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When localization changes (for example, PropertyName == "Item") update the header.
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Item")
        {
            UpdateHeaderText();
        }
    }

    // Updates the HeaderText value depending on the current selected section and localization.
    private void UpdateHeaderText()
    {
        HeaderText = SelectedSection switch
        {
            SectionType.Home => _localizationService["Nav_Home"],
            SectionType.Media => _localizationService["Nav_Media"],
            SectionType.History => _localizationService["Nav_History"],
            SectionType.Settings => _localizationService["Nav_Settings"],
            _ => "Atune",
        };
    }

    private void LoadInitialSettings()
    {
        var settings = _settingsService.LoadSettings();
        // Apply saved volume so slider reflects it and playback uses it
        Volume = settings.Volume;
        // Notify UI even if value didn't change
        OnPropertyChanged(nameof(Volume));
    }

    [RelayCommand]
    private void GoHome()
    {
        SelectedSection = SectionType.Home;
        HeaderText = _localizationService["Nav_Home"];
        CurrentView = _views[SectionType.Home];
    }

    [RelayCommand]
    private void GoMedia()
    {
        SelectedSection = SectionType.Media;
        HeaderText = _localizationService["Nav_Media"];
        CurrentView = _views[SectionType.Media];
    }

    [RelayCommand]
    private void GoHistory()
    {
        SelectedSection = SectionType.History;
        HeaderText = _localizationService["Nav_History"];
        CurrentView = _views[SectionType.History];
    }
    [RelayCommand]
    private void GoSettings()
    {
        SelectedSection = SectionType.Settings;
        CurrentView = _views[SectionType.Settings];
        HeaderText = _localizationService["Nav_Settings"];
    }

    [RelayCommand]
    private static void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    [RelayCommand]
    private void AddMedia()
    {
        if (CurrentView?.DataContext is MediaViewModel mediaVM)
        {
            mediaVM.AddMediaCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var query = SearchQuery?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(query))
            return;

        // Try to execute the navigation command using fuzzy matching
        if (TryGetNavigationCommand(query, out var navigationAction))
        {
            navigationAction!();
            SearchMessage = string.Empty;
            return;
        }

        // If the current view is associated with MediaViewModel, perform a search in the database
        if (CurrentView is UserControl view && view.DataContext is MediaViewModel mediaVM)
        {
            await mediaVM.SearchMediaCommand.ExecuteAsync(query);

            if (mediaVM.MediaItems.Count == 0)
            {
                var suggestions = GetSearchSuggestions(query);
                SearchMessage = "?????? ?? ???????. ????????, ?? ????? ? ????: " + string.Join(", ", suggestions);
            }
            else
            {
                SearchMessage = string.Empty;
            }
        }
        else
        {
            // If the current view does not match, switch to MediaView
            GoMedia();
            if (CurrentView is UserControl newView && newView.DataContext is MediaViewModel newMediaVM)
            {
                await newMediaVM.SearchMediaCommand.ExecuteAsync(query);

                if (newMediaVM.MediaItems.Count == 0)
                {
                    var suggestions = GetSearchSuggestions(query);
                    SearchMessage = "?????? ?? ???????. ????????, ?? ????? ? ????: " + string.Join(", ", suggestions);
                }
                else
                {
                    SearchMessage = string.Empty;
                }
            }
        }
    }

    // Tries to match the entered query with one of the navigation commands.
    // Uses keywords for each section with fuzzy comparison.
    private bool TryGetNavigationCommand(string query, out Action? navigationAction)
    {
        var navigationDict = new Dictionary<SectionType, Action>
        {
            { SectionType.Settings, GoSettings },
            { SectionType.Media, GoMedia },
            { SectionType.History, GoHistory },
            { SectionType.Home, GoHome }
        };

        // Get keywords through the provider
        var navigationKeywords = _keywordProvider.GetNavigationKeywords();

        double bestSimilarity = 0;
        SectionType bestMatch = SectionType.Home; // Default value
        bool found = false;

        foreach (var kvp in navigationKeywords)
        {
            foreach (var keyword in kvp.Value)
            {
                double similarity = CalculateSimilarity(query, keyword);
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestMatch = kvp.Key;
                    found = true;
                }
            }
        }

        if (found && bestSimilarity >= 0.6)
        {
            navigationAction = navigationDict[bestMatch];
            return true;
        }

        navigationAction = null;
        return false;
    }

    // Calculates the normalized similarity value between two strings (from 0 to 1),
    // where 1 means full match.
    private static double CalculateSimilarity(string source, string target)
    {
        int distance = LevenshteinDistance(source, target);
        int maxLength = Math.Max(source.Length, target.Length);
        if (maxLength == 0)
            return 1.0;
        double similarity = 1.0 - (double)distance / maxLength;

        // If the keyword starts with the query (if the query length >= 2 characters),
        // increase the similarity coefficient.
        if (source.Length >= 2 && target.StartsWith(source, StringComparison.InvariantCultureIgnoreCase))
        {
            similarity = Math.Max(similarity, 0.9);
        }

        return similarity;
    }

    // Calculates the Levenshtein distance between two strings.
    private static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t))
            return s.Length;

        int[,] d = new int[s.Length + 1, t.Length + 1];

        for (int i = 0; i <= s.Length; i++)
            d[i, 0] = i;
        for (int j = 0; j <= t.Length; j++)
            d[0, j] = j;

        for (int i = 1; i <= s.Length; i++)
        {
            for (int j = 1; j <= t.Length; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[s.Length, t.Length];
    }

    // Method for getting query variants based on similarity
    private IEnumerable<string> GetSearchSuggestions(string query)
    {
        var navigationKeywords = _keywordProvider.GetNavigationKeywords();
        var suggestionsList = new List<(string keyword, double similarity)>();

        foreach (var kvp in navigationKeywords)
        {
            foreach (var keyword in kvp.Value)
            {
                double similarity = CalculateSimilarity(query, keyword);
                if (similarity > 0.3) // threshold for showing suggestions (can be adjusted)
                {
                    suggestionsList.Add((keyword, similarity));
                }
            }
        }

        return suggestionsList.OrderByDescending(x => x.similarity)
                              .Select(x => x.keyword)
                              .Distinct()
                              .Take(3);
    }

    public bool CurrentPageIsMedia =>
        CurrentView is MediaView;

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            SearchSuggestions.Clear();
            IsSuggestionsOpen = false;
        }
        else
        {
            // Get suggestions based on the entered query.
            var suggestions = GetSearchSuggestions(value.ToLowerInvariant());
            SearchSuggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                SearchSuggestions.Add(suggestion);
            }
            IsSuggestionsOpen = SearchSuggestions.Any();
        }
    }

    // ?????, ??????????? ???????? PlayCommand
    [RelayCommand]
    private async Task ExecutePlayCommand()
    {
        // Delegate to playback service
        await _playbackService.Play();
    }

    // ??????????? ????? ??? ???????????? ??????????????? (play/pause)
    [RelayCommand]
    private void TogglePlayPause()
    {
        if (IsPlaying) _playbackService.Pause(); else _playbackService.Resume();
    }

    // ????????????? ?????????? ??? ????????? IsPlaying (???? ???????????? [ObservableProperty])
    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(PlayIconData));
    }

    [RelayCommand]
    private async Task ExecuteNextCommand()
    {
        await _playbackService.Next();
    }

    // ?????????? ????????????? ??? ????????? Volume; ????????? ???????? ? FFmpegService
    partial void OnVolumeChanged(int value)
    {
        _playbackService.Volume = value;
        _ = UpdateMetadataAsync();

        // Debounce final save after user finishes adjusting
        if (!_isInitializing)
        {
            _volumeSaveCts?.Cancel();
            _volumeSaveCts = new CancellationTokenSource();
            var token = _volumeSaveCts.Token;
            var volume = Volume;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500, token);
                    var settings = _settingsService.LoadSettings();
                    settings.Volume = volume;
                    _settingsService.SaveSettings(settings);
                    _logger.LogInformation("Settings saved (debounced)");
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving settings");
                }
            });
        }
    }

    private async void OnPlaybackEnded(object? sender, EventArgs e)
    {
        IsPlaying = false;
        await ExecuteNextCommand();
    }

    private void OnPlaybackStarted(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_playbackService == null)
                return;

            // Determine local file path from CurrentPath (could be file URI)
            var mrl = _playbackService.CurrentPath;
            var localPath = string.Empty;
            if (!string.IsNullOrEmpty(mrl))
            {
                localPath = mrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                    ? new Uri(mrl).LocalPath
                    : mrl;
            }

            // Update playback state
            CurrentMediaPath = localPath;
            IsPlaying = true;

            // Update current media item in Now Playing
            var mediaVm = _views[SectionType.Media].DataContext as MediaViewModel;
            CurrentMediaItem = mediaVm?.PlaybackQueue.FirstOrDefault(mi => string.Equals(mi.Path, localPath, StringComparison.OrdinalIgnoreCase));
        });
    }

    // Stubbed: no VLC metadata parsing, using DB-backed metadata instead
    private static Task UpdateMetadataAsync(bool force = false) => Task.CompletedTask;

    private static Bitmap CreateTransparentFallback()
    {
        // Create a small transparent bitmap as ultimate fallback
        var pixelSize = new PixelSize(1, 1);
        var dpi = new Vector(96, 96);
        var rtb = new RenderTargetBitmap(pixelSize, dpi);
        using (var ctx = rtb.CreateDrawingContext(false))
        {
            ctx.FillRectangle(Brushes.Transparent, new Rect(0, 0, pixelSize.Width, pixelSize.Height));
        }
        return rtb;
    }

    private void OnPlaybackPaused(object? sender, EventArgs e)
    {
        IsPlaying = false;
        // ?? ????????????? ??????, ?????? ????????? ?????????
    }

    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        if (_playbackService == null)
        {
            return;
        }

        try
        {
            // ?????? ????????? ???????, ???? ???? ??????????????? ?? ?????
            var newPosition = _playbackService.Position;
            if (newPosition != CurrentPosition)
            {
                CurrentPosition = newPosition;
            }

            var newDuration = _playbackService.Duration;
            if (newDuration != Duration)
            {
                Duration = newDuration;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media position");
        }
    }

    partial void OnCurrentPositionChanged(TimeSpan value)
    {
        if (_playbackService == null)
            return;
        // Clamp the new position to the track duration
        var target = value;
        if (Duration.TotalSeconds > 0 && value.TotalSeconds > Duration.TotalSeconds)
        {
            target = Duration;
        }
        // Only update engine if difference is significant
        if (Math.Abs((_playbackService.Position - target).TotalSeconds) > 0.5)
        {
            try
            {
                _playbackService.Position = target;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting media position");
                // Reflect clamped position in UI
                CurrentPosition = target;
            }
        }
    }

    // ?????????? ?????????????? CS1998 ??????????? await
    [RelayCommand]
    private async Task PlayAsync(string path)
    {
        try
        {
            await _playbackService.Play(new MediaItem(title: string.Empty, album: null, year: 0, genre: string.Empty, path: path, duration: TimeSpan.Zero, trackArtists: null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Playback error");
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _playbackService.Stop();

        CurrentPosition = TimeSpan.Zero;
        IsPlaying = false;
        _logger.LogInformation("Playback stopped");
    }

    [RelayCommand]
    private async Task Next()
    {
        await _playbackService.Next();
    }

    [RelayCommand]
    private async Task Previous()
    {
        await _playbackService.Previous();
    }

    // Command to play entire album via queue
    [RelayCommand]
    private async Task PlayAlbum(AlbumInfo album)
    {
        if (_views.TryGetValue(SectionType.Media, out var view) && view.DataContext is MediaViewModel mediaVm)
        {
            // clear existing queue
            mediaVm.ClearQueueCommand.Execute(null);
            // enqueue and play album
            await mediaVm.PlayAlbumCommand.ExecuteAsync(album);
        }
    }

    // Command to play from specific track within album
    [RelayCommand]
    private async Task PlayAlbumFromTrack(MediaItem track)
    {
        // Capture album context before switching to Media view
        AlbumViewModel? albumVm = null;
        if (CurrentView is AlbumView av && av.DataContext is AlbumViewModel aVm)
            albumVm = aVm;

        if (_views.TryGetValue(SectionType.Media, out var view) && view.DataContext is MediaViewModel mediaVm)
        {
            // Clear existing queue
            mediaVm.ClearQueueCommand.Execute(null);
            // Replace enqueue logic to add entire album and set starting track position
            if (albumVm != null)
            {
                var tracks = albumVm.Album.Tracks;
                int startIndex = tracks.IndexOf(track);
                foreach (var t in tracks)
                    mediaVm.AddToQueueCommand.Execute(t);
                mediaVm.SetQueuePositionCommand.Execute(startIndex);
            }
            else if (CurrentView is MediaView)
            {
                var tracks = mediaVm.MediaItems;
                int startIndex = tracks.IndexOf(track);
                if (startIndex < 0) startIndex = 0;
                foreach (var t in tracks)
                    mediaVm.AddToQueueCommand.Execute(t);
                mediaVm.SetQueuePositionCommand.Execute(startIndex);
            }
            else
            {
                mediaVm.AddToQueueCommand.Execute(track);
                mediaVm.SetQueuePositionCommand.Execute(0);
            }

            // Start playback of the first enqueued track
            await mediaVm.PlayNextInQueueCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand]
    private void GoPlaylist(Playlist playlist)
    {
        if (playlist == null) return;
        var playlistControl = new PlaylistView { DataContext = new PlaylistViewModel(playlist) };
        SelectedSection = SectionType.Media;
        CurrentView = playlistControl;
        HeaderText = playlist.Name;
    }
}
