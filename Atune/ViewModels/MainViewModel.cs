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
    private Bitmap? _coverArt;

    private DispatcherTimer _positionTimer;

    private Bitmap? _pendingCoverArt;
    private bool _coverArtLoading;

    public IRelayCommand PlayCommand { get; }
    public IRelayCommand StopCommand { get; }
    public IRelayCommand TogglePlayPauseCommand { get; }
    public IRelayCommand NextCommand { get; }
    public IRelayCommand PreviousCommand { get; }

    private readonly ISettingsService _settingsService;
    private readonly Dictionary<SectionType, Control> _views;
    private readonly Func<Type, ViewModelBase> _viewModelFactory;
    private readonly Func<Type, Control> _viewFactory;
    private readonly INavigationKeywordProvider _keywordProvider;
    private readonly LocalizationService _localizationService;
    private readonly MediaPlayerService _mediaPlayerService;
    private readonly ILogger<MainViewModel> _logger;

    // Константы с векторными данными для иконок
    private const string PlayIconPath = "M2 12C2 6.477 6.477 2 12 2s10 4.477 10 10-4.477 10-10 10S2 17.523 2 12Zm8.856-3.845A1.25 1.25 0 0 0 9 9.248v5.504a1.25 1.25 0 0 0 1.856 1.093l5.757-3.189a.75.75 0 0 0 0-1.312l-5.757-3.189Z";
    private const string PauseIconPath = "M5.746 3a1.75 1.75 0 0 0-1.75 1.75v14.5c0 .966.784 1.75 1.75 1.75h3.5a1.75 1.75 0 0 0 1.75-1.75V4.75A1.75 1.75 0 0 0 9.246 3h-3.5ZM14.746 3a1.75 1.75 0 0 0-1.75 1.75v14.5c0 .966.784 1.75 1.75 1.75h3.5a1.75 1.75 0 0 0 1.75-1.75V4.75A1.75 1.75 0 0 0 18.246 3h-3.5Z";

    // Вычисляемое свойство для передачи в XAML
    public string PlayIconData => IsPlaying ? PauseIconPath : PlayIconPath;

    public MainViewModel(
        ISettingsService settingsService,
        Func<Type, ViewModelBase> viewModelFactory,
        Func<Type, Control> viewFactory,
        INavigationKeywordProvider keywordProvider,
        LocalizationService localizationService,
        MediaPlayerService mediaPlayerService,
        ILogger<MainViewModel> logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        _viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        _keywordProvider = keywordProvider ?? throw new ArgumentNullException(nameof(keywordProvider));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        try
        {
            _mediaPlayerService = mediaPlayerService;
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

        // Инициализируем команду, которая вызывает метод воспроизведения
        PlayCommand = new RelayCommand(ExecutePlayCommand);
        StopCommand = new RelayCommand(ExecuteStopCommand);

        // Новый переключающий комманд
        TogglePlayPauseCommand = new RelayCommand(ExecuteTogglePlayPauseCommand);
        NextCommand = new RelayCommand(ExecuteNextCommand);
        PreviousCommand = new RelayCommand(ExecutePreviousCommand);

        _mediaPlayerService.PlaybackEnded += OnPlaybackEnded;
        
        // Загружаем настройки полностью, а не только Volume
        var settings = _settingsService.LoadSettings();
        Volume = settings.Volume; // Используем значение из файла настроек
        
        // Обновляем сервис плеера
        _mediaPlayerService.Volume = Volume;

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
        // Apply settings
        // Применяем настройки
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
        if (_views.TryGetValue(SectionType.Settings, out var vm))
        {
            SelectedSection = SectionType.Settings;
            HeaderText = _localizationService["Nav_Settings"];
            CurrentView = vm;
        }
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
    private void ExecutePlayCommand()
    {
        if (CurrentView is MediaView mediaView && 
            mediaView.DataContext is MediaViewModel mvm &&
            mvm.SelectedMediaItem != null)
        {
            _mediaPlayerService.Play(mvm.SelectedMediaItem.Path);
            IsPlaying = true;
        }
    }

    private void ExecuteStopCommand()
    {
        _mediaPlayerService.Stop();
        IsPlaying = false;
    }

    // Обновлённый метод для переключения воспроизведения (play/pause)
    private void ExecuteTogglePlayPauseCommand()
    {
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

    private void ExecuteNextCommand()
    {
        if (CurrentView is MediaView mediaView && 
            mediaView.DataContext is MediaViewModel mediaVM)
        {
            mediaVM.NextMediaItemCommand.Execute(null);
        }
    }

    private void ExecutePreviousCommand()
    {
        if (CurrentView is MediaView mediaView && 
            mediaView.DataContext is MediaViewModel mediaVM)
        {
            mediaVM.PreviousMediaItemCommand.Execute(null);
        }
    }

    // Вызывается автоматически при изменении Volume; обновляем значение в FFmpegService
    partial void OnVolumeChanged(int value)
    {
        _mediaPlayerService.Volume = value;
        
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

    private async void OnPlaybackStarted(object? sender, EventArgs e)
    {
        _positionTimer.Start();
        UpdateTimerInterval(active: true);
        
        if (_coverArtLoading) return;
        _coverArtLoading = true;
        
        try 
        {
            // Сначала пробуем загрузить встроенную обложку
            var embeddedCover = await Task.Run(() => 
                _mediaPlayerService?.GetEmbeddedCoverArt());
            
            if (embeddedCover != null)
            {
                using (embeddedCover)
                {
                    CoverArt = new Bitmap(embeddedCover);
                    return;
                }
            }

            // Если встроенной нет - загружаем из файла
            var coverPath = await Task.Run(() => 
                _mediaPlayerService?.GetCoverArtPath());
            
            if (!string.IsNullOrEmpty(coverPath) && File.Exists(coverPath))
            {
                CoverArt = new Bitmap(coverPath);
                return;
            }

            // Если ничего не найдено - используем дефолтную
            CoverArt = LoadDefaultCover();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cover art");
            CoverArt = LoadDefaultCover();
        }
        finally 
        {
            _coverArtLoading = false;
        }
    }

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
        UpdateTimerInterval(active: false);
    }

    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        if (_mediaPlayerService == null || !_mediaPlayerService.IsPlaying) 
        {
            _positionTimer.Stop();
            return;
        }

        try
        {
            // Обновляем позицию только если медиа активно
            var newPosition = _mediaPlayerService.Position;
            if (newPosition != CurrentPosition)
            {
                CurrentPosition = newPosition;
            }

            // Обновляем длительность только при изменении
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

    private void UpdateTimerInterval(bool active)
    {
        _positionTimer.Interval = active 
            ? TimeSpan.FromMilliseconds(250) // Частые обновления при активном воспроизведении
            : TimeSpan.FromMilliseconds(1000); // Редкие обновления при паузе
    }

    partial void OnCurrentPositionChanged(TimeSpan value)
    {
        if (_mediaPlayerService?.IsPlaying == true 
            && Math.Abs((_mediaPlayerService.Position - value).TotalSeconds) > 0.5)
        {
            try
            {
                _mediaPlayerService.Position = value;
                UpdateTimerInterval(active: true); // Сбрасываем интервал при взаимодействии
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting media position");
            }
        }
    }
}