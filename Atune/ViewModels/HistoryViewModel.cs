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

namespace Atune.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly LocalizationService _localizationService;

    // Chart data for LiveChartsCore
    [ObservableProperty]
    private ISeries[] _series;
    [ObservableProperty]
    private Axis[] _xAxes;
    [ObservableProperty]
    private Axis[] _yAxes;
    [ObservableProperty]
    private List<string> _rangeOptions;
    [ObservableProperty]
    private int _selectedRangeIndex;

    // Converter for formatting durations
    private readonly DurationConverter _durationConverter = new DurationConverter();
    // Computed text properties combining localization labels with values
    public string TotalPlaysLabelText => $"{_localizationService["History_TotalPlaysLabel"]}{TotalPlays}";
    public string PlaybackTimeLabelText => $"{_localizationService["History_PlaybackTimeLabel"]}{(string)_durationConverter.Convert(AverageDuration, typeof(string), null, CultureInfo.CurrentCulture)}";

    public HistoryViewModel(IMemoryCache cache, IUnitOfWork unitOfWork, LocalizationService localizationService)
    {
        _unitOfWork = unitOfWork;
        _localizationService = localizationService;
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

        LoadStatsCommand = new AsyncRelayCommand(LoadStatsAsync);
        _ = LoadStatsAsync();
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
            var allHistory = await _unitOfWork.PlayHistory.GetAllAsync();
            var now = DateTime.Now;
            DateTime rangeStart = now;
            var allList = allHistory.ToList();
            List<DateTimePoint> points;
            switch (SelectedRangeIndex)
            {
                case 0: // Last 24 hours
                    rangeStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(-23);
                    var hours24 = Enumerable.Range(0, 24).Select(i => rangeStart.AddHours(i)).ToList();
                    var dictHour24 = allList
                        .Where(h => h.PlayedAt >= rangeStart)
                        .GroupBy(h => new DateTime(h.PlayedAt.Year, h.PlayedAt.Month, h.PlayedAt.Day, h.PlayedAt.Hour, 0, 0))
                        .ToDictionary(g => g.Key, g => g.Count());
                    points = hours24.Select(dt => new DateTimePoint(dt, (double)(dictHour24.TryGetValue(dt, out var c) ? c : 0))).ToList();
                    break;
                case 1: // Last 3 days
                    rangeStart = now.AddDays(-3);
                    var startHour3d = new DateTime(rangeStart.Year, rangeStart.Month, rangeStart.Day, rangeStart.Hour, 0, 0);
                    var hours72 = Enumerable.Range(0, 72).Select(i => startHour3d.AddHours(i)).ToList();
                    var dictHour72 = allList
                        .Where(h => h.PlayedAt >= startHour3d)
                        .GroupBy(h => new DateTime(h.PlayedAt.Year, h.PlayedAt.Month, h.PlayedAt.Day, h.PlayedAt.Hour, 0, 0))
                        .ToDictionary(g => g.Key, g => g.Count());
                    points = hours72.Select(dt => new DateTimePoint(dt, (double)(dictHour72.TryGetValue(dt, out var c) ? c : 0))).ToList();
                    break;
                case 2: // Last week
                    rangeStart = now.Date.AddDays(-6);
                    var days7 = Enumerable.Range(0, 7).Select(i => rangeStart.AddDays(i)).ToList();
                    var dictDay7 = allList
                        .Where(h => h.PlayedAt.Date >= rangeStart)
                        .GroupBy(h => h.PlayedAt.Date)
                        .ToDictionary(g => g.Key, g => g.Count());
                    points = days7.Select(dt => new DateTimePoint(dt, (double)(dictDay7.TryGetValue(dt, out var c) ? c : 0))).ToList();
                    break;
                case 3: // Last month
                    rangeStart = now.Date.AddDays(-29);
                    var days30 = Enumerable.Range(0, 30).Select(i => rangeStart.AddDays(i)).ToList();
                    var dictDay30 = allList
                        .Where(h => h.PlayedAt.Date >= rangeStart)
                        .GroupBy(h => h.PlayedAt.Date)
                        .ToDictionary(g => g.Key, g => g.Count());
                    points = days30.Select(dt => new DateTimePoint(dt, (double)(dictDay30.TryGetValue(dt, out var c) ? c : 0))).ToList();
                    break;
                case 4: // Last quarter
                    rangeStart = now.Date.AddMonths(-3);
                    var dow = (int)rangeStart.DayOfWeek;
                    var monday = rangeStart.AddDays(-(dow == 0 ? 6 : dow - 1));
                    var weeksCount = (int)Math.Ceiling((now.Date - monday).TotalDays / 7) + 1;
                    var weeks = Enumerable.Range(0, weeksCount).Select(i => monday.AddDays(i * 7)).ToList();
                    var dictWeek = allList
                        .Where(h => h.PlayedAt.Date >= monday)
                        .GroupBy(h =>
                        {
                            var d = h.PlayedAt.Date;
                            var dDow = (int)d.DayOfWeek;
                            return d.AddDays(-(dDow == 0 ? 6 : dDow - 1));
                        })
                        .ToDictionary(g => g.Key, g => g.Count());
                    points = weeks.Select(dt => new DateTimePoint(dt, (double)(dictWeek.TryGetValue(dt, out var c) ? c : 0))).ToList();
                    break;
                case 5: // Last year
                    var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
                    var months = Enumerable.Range(0, 12).Select(i => startMonth.AddMonths(i)).ToList();
                    var dictMonth = allList
                        .Where(h => h.PlayedAt.Date >= startMonth)
                        .GroupBy(h => new DateTime(h.PlayedAt.Year, h.PlayedAt.Month, 1))
                        .ToDictionary(g => g.Key, g => g.Count());
                    points = months.Select(dt => new DateTimePoint(dt, (double)(dictMonth.TryGetValue(dt, out var c) ? c : 0))).ToList();
                    break;
                default:
                    goto case 2;
            }

            // Фильтрация для статистики: берём записи от rangeStart до now
            var periodFiltered = allList.Where(h => h.PlayedAt >= rangeStart && h.PlayedAt <= now).ToList();
            // Общее время воспроизведения
            AverageDuration = periodFiltered.Any() ? TimeSpan.FromSeconds(periodFiltered.Sum(h => h.DurationSeconds)) : TimeSpan.Zero;
            TotalPlays = (int)points.Sum(p => p.Value);
            // Формат меток оси X: время, дни, недели или месяцы
            var xFormat = SelectedRangeIndex <= 1             // последние 24 часа и 3 дня
                ? "HH:mm"                                   // показываем только часы и минуты
                : SelectedRangeIndex <= 3                     // неделя и месяц
                    ? "dd.MM"                              // день и месяц (например "12.02")
                    : SelectedRangeIndex == 4                // квартал
                        ? "dd MMM"                          // день и аббревиатура месяца (например "12 фев")
                        : "MM.yyyy";                        // год: месяц и год (например "02.2023")
            // Вычисляем шаг значений для отображения дат/времени на оси X
            var stepDays = SelectedRangeIndex <= 1
                ? TimeSpan.FromHours(1).TotalDays       // шаг 1 час
                : SelectedRangeIndex <= 3
                    ? TimeSpan.FromDays(1).TotalDays     // шаг 1 день
                    : SelectedRangeIndex == 4
                        ? TimeSpan.FromDays(7).TotalDays   // шаг 1 неделя
                        : TimeSpan.FromDays(30).TotalDays;  // шаг ~1 месяц
            // Формируем массив строк для оси X и категориальные точки
            var labels = points.Select(p => p.DateTime.ToString(xFormat)).ToArray();

            // Настраиваем серию с типом DateTimePoint (по умолчанию маппинг берёт дату и значение)
            Series = new ISeries[]
            {
                new LineSeries<DateTimePoint>
                {
                    Name = _localizationService["History_SeriesPlays"],
                    Values = points,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.CornflowerBlue, 2),
                    GeometryStroke = new SolidColorPaint(SKColors.White),
                    GeometryFill = new SolidColorPaint(SKColors.CornflowerBlue),
                    GeometrySize = 6
                }
            };

            // Настраиваем категориальную ось X
            XAxes = new[]
            {
                new Axis
                {
                    Name = _localizationService["History_AxisDate"],
                    NamePaint = new SolidColorPaint(new SKColor(180, 180, 180)),
                    LabelsPaint = new SolidColorPaint(new SKColor(180, 180, 180)),
                    // ограничиваем область и шаг оси по времени
                    MinLimit = points.First().DateTime.Ticks,
                    MaxLimit = points.Last().DateTime.Ticks,
                    MinStep = stepDays * TimeSpan.TicksPerDay,
                    // конвертация ticks в DateTime
                    Labeler = val =>
                    {
                        if (double.IsNaN(val) || double.IsInfinity(val)) return string.Empty;
                        try
                        {
                            return new DateTime((long)val).ToString(xFormat);
                        }
                        catch
                        {
                            return string.Empty;
                        }
                    },
                }
            };
            // Настраиваем ось Y: стандартная конфигурация
            YAxes = new[]
            {
                new Axis
                {
                    Name = _localizationService["History_AxisPlays"],
                    NamePaint = new SolidColorPaint(new SKColor(200, 200, 200)),
                    LabelsPaint = new SolidColorPaint(new SKColor(200, 200, 200))
                }
            };
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
}
