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
    private ObservableCollection<string> searchSuggestions = new ObservableCollection<string>();

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

    private DispatcherTimer _positionTimer;
    private DispatcherTimer? _metadataTimer;

    private bool _coverArtLoading;

    private readonly ISettingsService _settingsService;
    private readonly Dictionary<SectionType, Control> _views;
    private readonly Func<Type, ViewModelBase> _viewModelFactory;
    private readonly Func<Type, Control> _viewFactory;
    private readonly INavigationKeywordProvider _keywordProvider;
    private readonly LocalizationService _localizationService;
    private readonly MediaPlayerService _mediaPlayerService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly ICoverArtService _coverArtService;

    // Константы с векторными данными для иконок
    private const string PlayIconPath = "M5 5.274c0-1.707 1.826-2.792 3.325-1.977l12.362 6.726c1.566.853 1.566 3.101 0 3.953L8.325 20.702C6.826 21.518 5 20.432 5 18.726V5.274Z";

    private const string PauseIconPath = "M5.746 3a1.75 1.75 0 0 0-1.75 1.75v14.5c0 .966.784 1.75 1.75 1.75h3.5a1.75 1.75 0 0 0 1.75-1.75V4.75A1.75 1.75 0 0 0 9.246 3h-3.5ZM14.746 3a1.75 1.75 0 0 0-1.75 1.75v14.5c0 .966.784 1.75 1.75 1.75h3.5a1.75 1.75 0 0 0 1.75-1.75V4.75A1.75 1.75 0 0 0 18.246 3h-3.5Z";

    // Вычисляемое свойство для передачи в XAML
    public string PlayIconData => IsPlaying ? PauseIconPath : PlayIconPath;

    // Add at class level
    private DateTime _lastToggleTime = DateTime.MinValue;

    public MainViewModel(
        ISettingsService settingsService,
        Func<Type, ViewModelBase> viewModelFactory,
        Func<Type, Control> viewFactory,
        INavigationKeywordProvider keywordProvider,
        LocalizationService localizationService,
        MediaPlayerService mediaPlayerService,
        ILogger<MainViewModel> logger,
        ICoverArtService coverArtService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        _viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        _keywordProvider = keywordProvider ?? throw new ArgumentNullException(nameof(keywordProvider));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _coverArtService = coverArtService ?? throw new ArgumentNullException(nameof(coverArtService));
        
        try
        {
            _mediaPlayerService = mediaPlayerService;
            // Apply initial settings now that media player service is available
            LoadInitialSettings();
        }
        catch (MediaPlayerInitializationException ex)
        {
            _logger.LogCritical(ex, "Media player initialization failed");
            throw new CriticalStartupException(
                "Media components failed to initialize", ex);
        }
        
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
        // Подпишитесь на событие изменения локализации.
        _localizationService.PropertyChanged += LocalizationService_PropertyChanged;

        // Оптимизированный таймер
        _positionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250) // Уменьшили интервал для плавности
        };
        _positionTimer.Tick += PositionTimer_Tick;
        
        // Подписываемся на события состояния плеера
        _mediaPlayerService.PlaybackStarted += OnPlaybackStarted;
        _mediaPlayerService.PlaybackPaused += OnPlaybackPaused;
        
        _mediaPlayerService.PlaybackEnded += (s, e) => 
        {
            CurrentPosition = TimeSpan.Zero;
            Duration = TimeSpan.Zero;
        };

        _metadataTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
    }

    private void LocalizationService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When localization changes (for example, PropertyName == "Item") update the header.
        // При изменении локализации (например, PropertyName == "Item") обновите заголовок.
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Item")
        {
            UpdateHeaderText();
        }
    }

    // Updates the HeaderText value depending on the current selected section and localization.
    // Обновляет значение HeaderText в зависимости от текущего выбранного раздела и локализации.
    private void UpdateHeaderText()
    {
        switch (SelectedSection)
        {
            case SectionType.Home:
                HeaderText = _localizationService["Nav_Home"];
                break;
            case SectionType.Media:
                HeaderText = _localizationService["Nav_Media"];
                break;
            case SectionType.History:
                HeaderText = _localizationService["Nav_History"];
                break;
            case SectionType.Settings:
                HeaderText = _localizationService["Nav_Settings"];
                break;
            default:
                HeaderText = "Atune";
                break;
        }
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
    private void Exit()
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
        // Попробуйте выполнить команду навигации с использованием нечеткого сопоставления
        if (TryGetNavigationCommand(query, out var navigationAction))
        {
            navigationAction!();
            SearchMessage = string.Empty;
            return;
        }

        // If the current view is associated with MediaViewModel, perform a search in the database
        // Если текущий вид связан с MediaViewModel, выполните поиск в базе данных
        if (CurrentView is UserControl view && view.DataContext is MediaViewModel mediaVM)
        {
            await mediaVM.SearchMediaCommand.ExecuteAsync(query);

            if (mediaVM.MediaItems.Count == 0)
            {
                var suggestions = GetSearchSuggestions(query);
                SearchMessage = "Ничего не найдено. Возможно, вы имели в виду: " + string.Join(", ", suggestions);
            }
            else
            {
                SearchMessage = string.Empty;
            }
        }
        else
        {
            // If the current view does not match, switch to MediaView
            // Если текущий вид не соответствует, переключитесь на MediaView
            GoMedia();
            if (CurrentView is UserControl newView && newView.DataContext is MediaViewModel newMediaVM)
            {
                await newMediaVM.SearchMediaCommand.ExecuteAsync(query);

                if (newMediaVM.MediaItems.Count == 0)
                {
                    var suggestions = GetSearchSuggestions(query);
                    SearchMessage = "Ничего не найдено. Возможно, вы имели в виду: " + string.Join(", ", suggestions);
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
    // <param name="query">The user's query converted to lowercase.</param>
    // <param name="navigationAction">The returned navigation action if a match is found.</param>
    // <returns>True, if a suitable match is found, otherwise false.</returns>

    // Метод для попытки сопоставления введенного запроса с одной из команд навигации.
    // Использует ключевые слова для каждого раздела с нечетким сравнением.
    // <param name="query">Введенный пользователем запрос, преобразованный в нижний регистр.</param>
    // <param name="navigationAction">Возвращаемая команда навигации, если найдено совпадение.</param>
    // <returns>True, если найдено подходящее совпадение, иначе false.</returns>
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
        // Получаем ключевые слова через поставщика
        var navigationKeywords = _keywordProvider.GetNavigationKeywords();

        double bestSimilarity = 0;
        SectionType bestMatch = SectionType.Home; // Default value / Значение по умолчанию
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
    // Метод для вычисления нормализованного значения сходства между двумя строками (от 0 до 1),
    // где 1 означает полное совпадение.
    private double CalculateSimilarity(string source, string target)
    {
        int distance = LevenshteinDistance(source, target);
        int maxLength = Math.Max(source.Length, target.Length);
        if (maxLength == 0)
            return 1.0;
        double similarity = 1.0 - (double)distance / maxLength;

        // If the keyword starts with the query (if the query length >= 2 characters),
        // increase the similarity coefficient.
        // Если ключевое слово начинается с запроса (если длина запроса >= 2 символов),
        // увеличиваем коэффициент сходства.
        if (source.Length >= 2 && target.StartsWith(source, StringComparison.InvariantCultureIgnoreCase))
        {
            similarity = Math.Max(similarity, 0.9);
        }

        return similarity;
    }

    // Calculates the Levenshtein distance between two strings.
    // Метод для вычисления расстояния Левенштейна между двумя строками.
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
    // Метод для получения вариантов запроса на основе сходства
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
            // Получаем предложения на основе введенного запроса.
            var suggestions = GetSearchSuggestions(value.ToLowerInvariant());
            SearchSuggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                SearchSuggestions.Add(suggestion);
            }
            IsSuggestionsOpen = SearchSuggestions.Any();
        }
    }

    // Метод, исполняемый командой PlayCommand
    [RelayCommand]
    private async void ExecutePlayCommand()
    {
        if (CurrentView is MediaView mediaView && 
            mediaView.DataContext is MediaViewModel mvm &&
            mvm.SelectedMediaItem != null)
        {
            // Apply the current UI volume setting before playback
            _mediaPlayerService.Volume = Volume;
            await _mediaPlayerService.Play(mvm.SelectedMediaItem.Path);
            IsPlaying = true;
        }
    }

    private void ExecuteStopCommand()
    {
        _mediaPlayerService.Pause();
        IsPlaying = false;
    }

    // Обновлённый метод для переключения воспроизведения (play/pause)
    [RelayCommand]
    private void TogglePlayPause()
    {
        // In TogglePlayPause method, debounce rapid toggles
        if ((DateTime.UtcNow - _lastToggleTime).TotalMilliseconds < 250)
        {
            return; // ignore toggles within 250ms
        }
        _lastToggleTime = DateTime.UtcNow;

        // Immediately reflect state in UI
        if (_mediaPlayerService.IsPlaying)
        {
            _mediaPlayerService.Pause();
            IsPlaying = false;
        }
        else
        {
            _mediaPlayerService.Resume();
            IsPlaying = true;
        }
    }

    // Автоматически вызывается при изменении IsPlaying (если используется [ObservableProperty])
    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(PlayIconData));
    }

    [RelayCommand]
    private void ExecuteNextCommand()
    {
        if (CurrentView is MediaView mediaView && 
            mediaView.DataContext is MediaViewModel mediaVM)
        {
            mediaVM.PlayNextInQueueCommand.Execute(null);
        }
    }

    private void ExecutePreviousCommand()
    {
        if (CurrentView is MediaView mediaView && 
            mediaView.DataContext is MediaViewModel mediaVM)
        {
            // Use playback queue for previous track
            mediaVM.PlayNextInQueueCommand.Execute(null);
        }
    }

    // Вызывается автоматически при изменении Volume; обновляем значение в FFmpegService
    partial void OnVolumeChanged(int value)
    {
        _mediaPlayerService.Volume = value;
        _ = UpdateMetadataAsync();
        
        // Сохраняем через обновление полных настроек
        var settings = _settingsService.LoadSettings();
        settings.Volume = value;
        _settingsService.SaveSettings(settings);
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        IsPlaying = false;
        // Автоматический переход к следующему треку
        ExecuteNextCommand();
    }

    private void OnPlaybackStarted(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_mediaPlayerService == null || _positionTimer == null)
                return;

            // Determine local file path from CurrentPath (could be file URI)
            var mrl = _mediaPlayerService.CurrentPath;
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
            _positionTimer.Start();
            // Apply main volume setting when playback actually starts
            _mediaPlayerService.Volume = Volume;

            // Update current media item in Now Playing
            var mediaVm = _views[SectionType.Media].DataContext as MediaViewModel;
            CurrentMediaItem = mediaVm?.PlaybackQueue.FirstOrDefault(mi => string.Equals(mi.Path, localPath, StringComparison.OrdinalIgnoreCase));
        });
    }

    // Stubbed: no VLC metadata parsing, using DB-backed metadata instead
    private Task UpdateMetadataAsync(bool force = false) => Task.CompletedTask;

    private Bitmap? LoadCoverSafely(string path)
    {
        try 
        {
            return File.Exists(path) ? new Bitmap(path) : null;
        }
        catch 
        {
            return null;
        }
    }

    private Bitmap LoadDefaultCover()
    {
        try 
        {
            var asset = AssetLoader.Open(new Uri("avares://Atune/Assets/default_cover.jpg"));
            return new Bitmap(asset);
        }
        catch (System.IO.FileNotFoundException)
        {
            return CreateTransparentFallback();
        }
    }

    private Bitmap CreateTransparentFallback()
    {
        // Implementation of CreateTransparentFallback method
        // Реализация метода CreateTransparentFallback
        throw new NotImplementedException();
    }

    private void OnPlaybackPaused(object? sender, EventArgs e)
    {
        IsPlaying = false;
        // Не останавливаем таймер, только обновляем состояние
    }

    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        if (_mediaPlayerService == null) 
        {
            _positionTimer.Stop();
            return;
        }

        try
        {
            // Всегда обновляем позицию, даже если воспроизведение на паузе
            var newPosition = _mediaPlayerService.Position;
            if (newPosition != CurrentPosition)
            {
                CurrentPosition = newPosition;
            }

            var newDuration = _mediaPlayerService.Duration;
            if (newDuration != Duration)
            {
                Duration = newDuration;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media position");
            _positionTimer.Stop();
        }
    }

    partial void OnCurrentPositionChanged(TimeSpan value)
    {
        // Убираем проверку на IsPlaying, разрешаем изменение позиции всегда
        if (_mediaPlayerService != null 
            && Math.Abs((_mediaPlayerService.Position - value).TotalSeconds) > 0.5)
        {
            try
            {
                _mediaPlayerService.Position = value;
                // Не меняем интервал таймера при ручной перемотке
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting media position");
            }
        }
    }

    private async void LoadMediaMetadata()
    {
        try
        {
            if (_mediaPlayerService == null) return;
            
            if (_metadataTimer != null)
            {
                _metadataTimer.Stop();
                _metadataTimer = null;
            }
            
            await Task.Run(() => _mediaPlayerService.GetCurrentMetadataAsync());
            await Task.Delay(500); 
            
            await UpdateMetadataAsync();
            
            _metadataTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _metadataTimer.Tick += async (s, e) => await UpdateMetadataAsync();
            _metadataTimer.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Metadata loading failed");
        }
    }

    // Исправляем предупреждение CS1998 добавлением await
    [RelayCommand]
    private async Task PlayAsync(string path)
    {
        try
        {
            _logger.LogInformation($"PlayAsync started for: {path}");
            
            CurrentMediaPath = path;
            await _mediaPlayerService.Play(path);
            IsPlaying = true;
            
            _logger.LogDebug("Starting metadata update delay");
            await Task.Delay(1000); // Увеличиваем задержку для инициализации
            await UpdateMetadataAsync();
            
            _logger.LogInformation("Playback started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Playback error");
        }
    }

    [RelayCommand]
    private void Stop()
    {
        if (_mediaPlayerService == null)
            return;

        // Stop playback if it's currently playing
        if (_mediaPlayerService.IsPlaying)
            _mediaPlayerService.Pause();

        // Rewind to track start
        _mediaPlayerService.Position = TimeSpan.Zero;

        // Update UI playback state
        CurrentPosition = TimeSpan.Zero;
        IsPlaying = false;

        _logger.LogInformation("Playback stopped and rewound to start");
    }

    [RelayCommand]
    private async Task Next()
    {
        try 
        {
            var mediaView = _views[SectionType.Media].DataContext as MediaViewModel;
            if (mediaView != null)
            {
                await mediaView.PlayNextInQueueCommand.ExecuteAsync(null);
            }
            await UpdateMetadataAsync(true);
            _logger.LogInformation("Next track played");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Next track error");
        }
    }

    [RelayCommand]
    private async Task Previous()
    {
        try 
        {
            var mediaView = _views[SectionType.Media].DataContext as MediaViewModel;
            if (mediaView == null) return;

            if (CurrentPosition.TotalSeconds >= 10)
            {
                // Перемотка в начало текущего трека
                _mediaPlayerService.Position = TimeSpan.Zero;
                CurrentPosition = TimeSpan.Zero;
                _logger.LogInformation("Rewind to start of current track");
            }
            else
            {
                // Переход к предыдущему треку в очереди
                await mediaView.PlayPreviousInQueueCommand.ExecuteAsync(null);
                await UpdateMetadataAsync(true);
                _logger.LogInformation("Previous track played");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Previous track error");
        }
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