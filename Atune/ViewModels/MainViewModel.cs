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

    private readonly ISettingsService _settingsService;
    private readonly Dictionary<SectionType, Control> _views;
    private readonly Func<Type, ViewModelBase> _viewModelFactory;
    private readonly Func<Type, Control> _viewFactory;
    private readonly INavigationKeywordProvider _keywordProvider;
    private readonly LocalizationService _localizationService;

    public MainViewModel(
        ISettingsService settingsService,
        Func<Type, ViewModelBase> viewModelFactory,
        Func<Type, Control> viewFactory,
        INavigationKeywordProvider keywordProvider,
        LocalizationService localizationService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        _viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
        _keywordProvider = keywordProvider ?? throw new ArgumentNullException(nameof(keywordProvider));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        
        _views = new Dictionary<SectionType, Control>
        {
            [SectionType.Home] = viewFactory(typeof(HomeView)),
            [SectionType.Media] = viewFactory(typeof(MediaView)),
            [SectionType.History] = viewFactory(typeof(HistoryView)),
            [SectionType.Settings] = viewFactory(typeof(SettingsView))
        };
        
        CurrentView = _views[SectionType.Home];
        HeaderText = _localizationService["Nav_Home"];

        // Подписываемся на событие изменения локализации.
        _localizationService.PropertyChanged += LocalizationService_PropertyChanged;
    }

    private void LocalizationService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // При изменении локализации (например, PropertyName == "Item") обновляем заголовок.
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Item")
        {
            UpdateHeaderText();
        }
    }

    /// <summary>
    /// Обновляет значение HeaderText в зависимости от текущего выбранного раздела и локализации.
    /// </summary>
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

        // Пытаемся выполнить навигационную команду по запросу с использованием fuzzy matching
        if (TryGetNavigationCommand(query, out var navigationAction))
        {
            navigationAction!();
            SearchMessage = string.Empty;
            return;
        }

        // Если текущий вид связан с MediaViewModel, выполняем поиск в базе данных
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
            // Если текущий вид не соответствует, переключаемся на MediaView
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

    /// <summary>
    /// Пытается сопоставить введённый запрос с одной из навигационных команд.
    /// Используются ключевые слова для каждого раздела с fuzzy-сравнением.
    /// </summary>
    /// <param name="query">Преобразованный в нижний регистр запрос пользователя.</param>
    /// <param name="navigationAction">Возвращаемое действие навигации, если сопоставление удалось.</param>
    /// <returns>True, если найдено подходящее совпадение, иначе false.</returns>
    private bool TryGetNavigationCommand(string query, out Action? navigationAction)
    {
        var navigationDict = new Dictionary<SectionType, Action>
        {
            { SectionType.Settings, GoSettings },
            { SectionType.Media, GoMedia },
            { SectionType.History, GoHistory },
            { SectionType.Home, GoHome }
        };

        // Получаем ключевые слова через провайдер
        var navigationKeywords = _keywordProvider.GetNavigationKeywords();

        double bestSimilarity = 0;
        SectionType bestMatch = SectionType.Home; // Значение по умолчанию
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

    /// <summary>
    /// Рассчитывает нормализованное значение похожести между двумя строками (от 0 до 1),
    /// где 1 означает полное совпадение.
    /// </summary>
    private double CalculateSimilarity(string source, string target)
    {
        int distance = LevenshteinDistance(source, target);
        int maxLength = Math.Max(source.Length, target.Length);
        if (maxLength == 0)
            return 1.0;
        double similarity = 1.0 - (double)distance / maxLength;

        // Если ключевое слово начинается с запроса (при длине запроса >= 2 символов),
        // повышаем коэффициент похожести.
        if (source.Length >= 2 && target.StartsWith(source, StringComparison.InvariantCultureIgnoreCase))
        {
            similarity = Math.Max(similarity, 0.9);
        }

        return similarity;
    }

    /// <summary>
    /// Вычисляет расстояние Левенштейна между двумя строками.
    /// </summary>
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

    // Метод для получения вариантов запроса на основе схожести
    private IEnumerable<string> GetSearchSuggestions(string query)
    {
        var navigationKeywords = _keywordProvider.GetNavigationKeywords();
        var suggestionsList = new List<(string keyword, double similarity)>();

        foreach (var kvp in navigationKeywords)
        {
            foreach (var keyword in kvp.Value)
            {
                double similarity = CalculateSimilarity(query, keyword);
                if (similarity > 0.3) // порог для показа подсказок (можно настроить)
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
            // Получаем подсказки на основе введённого запроса.
            var suggestions = GetSearchSuggestions(value.ToLowerInvariant());
            SearchSuggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                SearchSuggestions.Add(suggestion);
            }
            IsSuggestionsOpen = SearchSuggestions.Any();
        }
    }
}