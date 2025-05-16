using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Caching.Memory;
using Atune.Data.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.Defaults;
using System.Collections.Generic;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Media;
using Atune.Converters;
using System.Globalization;
using System.Collections.ObjectModel;
using Atune.Models.Dtos;
using Atune.Models;
using Atune.Services;
using Microsoft.Extensions.Logging;

namespace Atune.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly IMemoryCache _memoryCache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LocalizationService _localizationService;
    private readonly IPlaybackService _playbackService;
    private readonly IMediaRepository _mediaRepository;
    private readonly ILogger<HistoryViewModel> _logger;

    // Chart data for LiveChartsCore
    [ObservableProperty]
    private ISeries[] _series;
    [ObservableProperty]
    private Axis[]? _xAxes;
    [ObservableProperty]
    private Axis[]? _yAxes;
    [ObservableProperty]
    private List<string> _rangeOptions;
    [ObservableProperty]
    private int _selectedRangeIndex;

    // Converter for formatting durations
    private readonly DurationConverter _durationConverter = new DurationConverter();
    // Computed text properties combining localization labels with values
    public string TotalPlaysLabelText => $"{_localizationService["History_TotalPlaysLabel"]}{TotalPlays}";
    public string PlaybackTimeLabelText => $"{_localizationService["History_PlaybackTimeLabel"]}{(_durationConverter.Convert(AverageDuration, typeof(string), null, CultureInfo.CurrentCulture) as string ?? string.Empty)}";

    public HistoryViewModel(
        IMemoryCache memoryCache,
        IUnitOfWork unitOfWork,
        LocalizationService localizationService,
        IPlaybackService playbackService,
        IMediaRepository mediaRepository,
        ILogger<HistoryViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(playbackService);
        ArgumentNullException.ThrowIfNull(mediaRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _memoryCache = memoryCache;
        _unitOfWork = unitOfWork;
        _localizationService = localizationService;
        _playbackService = playbackService;
        _mediaRepository = mediaRepository;
        _logger = logger;
        // Обновляем при смене языка
        _localizationService.PropertyChanged += async (s, e) => await LoadStatsAsync();
        FromDate = DateTime.UtcNow.Date.AddDays(-7);
        ToDate = DateTime.UtcNow.Date;
        RangeOptions = new List<string>
        {
            _localizationService["History_RangeLast24Hours"],
            _localizationService["History_RangeLast3Days"],
            _localizationService["History_RangeLastWeek"],
            _localizationService["History_RangeLastMonth"],
            _localizationService["History_RangeLastQuarter"],
            _localizationService["History_RangeLastYear"]
        };
        SelectedRangeIndex = 2;
        // Initialize axes for date vs plays
        // Axes will be initialized dynamically in LoadStatsAsync based on selection
        Series = Array.Empty<ISeries>();
        XAxes = Array.Empty<Axis>();
        YAxes = Array.Empty<Axis>();
        LoadStatsCommand = new AsyncRelayCommand(LoadStatsAsync);
        _ = LoadStatsAsync();
        LoadRecentTracksCommand = new AsyncRelayCommand(LoadRecentTracksAsync);
        _ = LoadRecentTracksAsync();
    }

    [ObservableProperty]
    private DateTime _fromDate;

    [ObservableProperty]
    private DateTime _toDate;

    [ObservableProperty]
    private int _totalPlays;

    [ObservableProperty]
    private DateTime? _firstPlayDate;

    [ObservableProperty]
    private DateTime? _lastPlayDate;

    [ObservableProperty]
    private TimeSpan _averageDuration;

    [ObservableProperty]
    private bool _isBusy;

    public IAsyncRelayCommand LoadStatsCommand { get; }

    private async Task LoadStatsAsync()
    {
        try
        {
            IsBusy = true;
            _logger.LogInformation($"[History] Start LoadStatsAsync, SelectedRangeIndex={SelectedRangeIndex}");
            var allHistory = await _unitOfWork.PlayHistory.GetAllAsync();
            var now = DateTime.Now;
            DateTime rangeStart = now;
            var allList = allHistory.ToList();
            _logger.LogInformation($"[History] allList.Count={allList.Count}");
            // --- Новый подход: строгое количество точек и подписей по схеме пользователя ---
            List<(string Label, double Value)> chartPoints = new();
            switch (SelectedRangeIndex)
            {
                case 0: // 24 часа, 24 точки по часу
                {
                    var nowHour = DateTime.Now;
                    var start = nowHour.AddHours(-23).Date.AddHours(nowHour.Hour - 23);
                    for (int i = 0; i < 24; i++)
                    {
                        var hour = start.AddHours(i);
                        var count = allList.Count(h => h.PlayedAt >= hour && h.PlayedAt < hour.AddHours(1));
                        chartPoints.Add((hour.ToString("HH:00"), count));
                    }
                    break;
                }
                case 1: // 3 дня, 12 точек (по 4 периода на день)
                {
                    var nowDay = DateTime.Now.Date;
                    var start = nowDay.AddDays(-2);
                    string[] slots = { "Night", "Morning", "Day", "Evening" };
                    int[] slotStarts = { 0, 6, 12, 18 };
                    for (int d = 0; d < 3; d++)
                    {
                        var day = start.AddDays(d);
                        for (int s = 0; s < 4; s++)
                        {
                            var slotStart = day.AddHours(slotStarts[s]);
                            var slotEnd = slotStart.AddHours(6);
                            var count = allList.Count(h => h.PlayedAt >= slotStart && h.PlayedAt < slotEnd);
                            chartPoints.Add(($"{day:dd.MM} {slots[s]}", count));
                        }
                    }
                    break;
                }
                case 2: // Неделя, 7 точек по дням
                {
                    var start = DateTime.Now.Date.AddDays(-6);
                    for (int i = 0; i < 7; i++)
                    {
                        var day = start.AddDays(i);
                        var count = allList.Count(h => h.PlayedAt.Date == day);
                        chartPoints.Add((day.ToString("dd.MM"), count));
                    }
                    break;
                }
                case 3: // Месяц, 30 точек по дням
                {
                    var start = DateTime.Now.Date.AddDays(-29);
                    for (int i = 0; i < 30; i++)
                    {
                        var day = start.AddDays(i);
                        var count = allList.Count(h => h.PlayedAt.Date == day);
                        chartPoints.Add((day.ToString("dd.MM"), count));
                    }
                    break;
                }
                case 4: // Квартал, 12 точек (по неделям, 4 на месяц)
                {
                    var nowQ = DateTime.Now.Date;
                    var start = nowQ.AddMonths(-2).AddDays(-((int)nowQ.DayOfWeek - 1));
                    for (int i = 0; i < 12; i++)
                    {
                        var weekStart = start.AddDays(i * 7);
                        var weekEnd = weekStart.AddDays(7);
                        var count = allList.Count(h => h.PlayedAt.Date >= weekStart && h.PlayedAt.Date < weekEnd);
                        chartPoints.Add(($"{weekStart:dd.MM}-{weekEnd.AddDays(-1):dd.MM}", count));
                    }
                    break;
                }
                case 5: // Год, 12 точек по месяцам
                {
                    var nowY = DateTime.Now;
                    var start = new DateTime(nowY.Year, nowY.Month, 1).AddMonths(-11);
                    for (int i = 0; i < 12; i++)
                    {
                        var month = start.AddMonths(i);
                        var count = allList.Count(h => h.PlayedAt.Year == month.Year && h.PlayedAt.Month == month.Month);
                        chartPoints.Add((month.ToString("MM.yyyy"), count));
                    }
                    break;
                }
            }
            _logger.LogInformation($"[History] chartPoints.Count={chartPoints.Count}, labels=[{string.Join(", ", chartPoints.Select(p => p.Label))}]");
            // Формируем Series и XAxes
            XAxes = null; // Явный сброс оси X для LiveChartsCore
            var indexedPoints = chartPoints.Select((p, i) => new ObservablePoint(i, p.Value)).ToList();
            Series = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Name = _localizationService["History_SeriesPlays"],
                    Values = indexedPoints,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue, 2),
                    GeometryStroke = new SolidColorPaint(SKColors.White),
                    GeometryFill = new SolidColorPaint(SKColors.CornflowerBlue),
                    GeometrySize = 6
                }
            };
            OnPropertyChanged(nameof(Series));
            XAxes = new[]
            {
                new Axis
                {
                    Name = _localizationService["History_AxisDate"],
                    NamePaint = new SolidColorPaint(new SKColor(194, 194, 194)),
                    LabelsPaint = new SolidColorPaint(new SKColor(194, 194, 194)),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(64, 64, 64)),
                    LabelsRotation = -45,
                    Labels = chartPoints.Select(p => p.Label).ToArray(),
                    MinStep = 1,
                    ForceStepToMin = true,
                }
            };
            OnPropertyChanged(nameof(XAxes));

            // Настраиваем ось Y: стандартная конфигурация
            YAxes = new[]
            {
                new Axis
                {
                    Name = _localizationService["History_AxisPlays"],
                    NamePaint = new SolidColorPaint(new SKColor(194, 194, 194)),
                    LabelsPaint = new SolidColorPaint(new SKColor(194, 194, 194)),
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(64, 64, 64)),
                }
            };
            OnPropertyChanged(nameof(YAxes));

            if (_playbackService != null)
            {
                var duration = _playbackService.Duration;
                if (duration != AverageDuration)
                {
                    AverageDuration = duration;
                }
            }

            // Обновляем TotalPlays и AverageDuration только по текущему диапазону
            TotalPlays = (int)chartPoints.Sum(p => p.Value);
            // Для длительности: ищем все записи, попавшие в диапазон chartPoints
            List<DateTime> rangeDates = new();
            switch (SelectedRangeIndex)
            {
                case 0: // 24 часа
                    var nowHour = DateTime.Now;
                    var startHour = nowHour.AddHours(-23).Date.AddHours(nowHour.Hour - 23);
                    rangeDates = Enumerable.Range(0, 24).Select(i => startHour.AddHours(i)).ToList();
                    break;
                case 1: // 3 дня
                    var nowDay = DateTime.Now.Date;
                    var startDay = nowDay.AddDays(-2);
                    rangeDates = Enumerable.Range(0, 3).SelectMany(d =>
                        Enumerable.Range(0, 4).Select(s => startDay.AddDays(d).AddHours(s * 6))).ToList();
                    break;
                case 2: // Неделя
                    var startW = DateTime.Now.Date.AddDays(-6);
                    rangeDates = Enumerable.Range(0, 7).Select(i => startW.AddDays(i)).ToList();
                    break;
                case 3: // Месяц
                    var startM = DateTime.Now.Date.AddDays(-29);
                    rangeDates = Enumerable.Range(0, 30).Select(i => startM.AddDays(i)).ToList();
                    break;
                case 4: // Квартал
                    var nowQ = DateTime.Now.Date;
                    var startQ = nowQ.AddMonths(-2).AddDays(-((int)nowQ.DayOfWeek - 1));
                    rangeDates = Enumerable.Range(0, 12).Select(i => startQ.AddDays(i * 7)).ToList();
                    break;
                case 5: // Год
                    var nowY = DateTime.Now;
                    var startY = new DateTime(nowY.Year, nowY.Month, 1).AddMonths(-11);
                    rangeDates = Enumerable.Range(0, 12).Select(i => startY.AddMonths(i)).ToList();
                    break;
            }
            // Фильтруем только те записи, что попали в диапазон (по PlayedAt)
            IEnumerable<PlayHistory> filtered = allList;
            if (SelectedRangeIndex == 0)
            {
                var start = rangeDates.First();
                var end = rangeDates.Last().AddHours(1);
                filtered = allList.Where(h => h.PlayedAt >= start && h.PlayedAt < end);
            }
            else if (SelectedRangeIndex == 1)
            {
                var start = rangeDates.First();
                var end = rangeDates.Last().AddHours(6);
                filtered = allList.Where(h => h.PlayedAt >= start && h.PlayedAt < end);
            }
            else if (SelectedRangeIndex == 2 || SelectedRangeIndex == 3)
            {
                var start = rangeDates.First();
                var end = rangeDates.Last().AddDays(1);
                filtered = allList.Where(h => h.PlayedAt.Date >= start && h.PlayedAt.Date < end);
            }
            else if (SelectedRangeIndex == 4)
            {
                var start = rangeDates.First();
                var end = rangeDates.Last().AddDays(7);
                filtered = allList.Where(h => h.PlayedAt.Date >= start && h.PlayedAt.Date < end);
            }
            else if (SelectedRangeIndex == 5)
            {
                var start = rangeDates.First();
                var end = rangeDates.Last().AddMonths(1);
                filtered = allList.Where(h => h.PlayedAt >= start && h.PlayedAt < end);
            }
            AverageDuration = filtered.Any() ? TimeSpan.FromSeconds(filtered.Sum(h => h.DurationSeconds)) : TimeSpan.Zero;
            OnPropertyChanged(nameof(TotalPlays));
            OnPropertyChanged(nameof(AverageDuration));
        }
        finally
        {
            IsBusy = false;
        }
    }
    partial void OnSelectedRangeIndexChanged(int value)
    {
        _ = LoadStatsAsync();
    }
    partial void OnTotalPlaysChanged(int value)
    {
        OnPropertyChanged(nameof(TotalPlaysLabelText));
    }
    partial void OnAverageDurationChanged(TimeSpan value)
    {
        OnPropertyChanged(nameof(PlaybackTimeLabelText));
    }

    [ObservableProperty]
    private ObservableCollection<RecentTrackDto> _recentTracks = new();

    [ObservableProperty]
    private List<string> _sortOptions = new List<string> { "Сначала новые", "Сначала старые" };

    [ObservableProperty]
    private int _selectedSortIndex;

    public IAsyncRelayCommand LoadRecentTracksCommand { get; }

    partial void OnSelectedSortIndexChanged(int value)
    {
        _ = LoadRecentTracksAsync();
    }

    private async Task LoadRecentTracksAsync()
    {
        try
        {
            var all = await _unitOfWork.PlayHistory.GetAllAsync();
            IEnumerable<PlayHistory> sorted = SelectedSortIndex == 0
                ? all.OrderByDescending(h => h.PlayedAt)
                : all.OrderBy(h => h.PlayedAt);
            var dtos = sorted.Select(ph => new RecentTrackDto
            {
                Id = ph.MediaItem.Id,
                Title = ph.MediaItem.Title,
                CoverArtPath = ph.MediaItem.CoverArt,
                ArtistName = ph.MediaItem.TrackArtists.FirstOrDefault()?.Artist.Name ?? string.Empty,
                LastPlayedAt = ph.PlayedAt
            }).ToList();
            RecentTracks = new ObservableCollection<RecentTrackDto>(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recent tracks");
        }
    }

    [RelayCommand]
    private async Task PlayRecentTrack(RecentTrackDto dto)
    {
        _playbackService.ClearQueue();
        var orders = SelectedSortIndex == 0
            ? RecentTracks.OrderByDescending(r => r.LastPlayedAt)
            : RecentTracks.OrderBy(r => r.LastPlayedAt);
        var orderedList = orders.ToList();
        var mediaItems = new List<MediaItem>();
        foreach (var r in orderedList)
        {
            var media = await _mediaRepository.GetByIdAsync(r.Id);
            if (media != null)
                mediaItems.Add(media);
        }
        foreach (var m in mediaItems)
            _playbackService.Enqueue(m);
        var index = orderedList.FindIndex(r => r.Id == dto.Id);
        await _playbackService.PlayAtIndex(index);
    }
}
