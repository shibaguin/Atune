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

namespace Atune.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;

    // Chart data for LiveChartsCore
    [ObservableProperty]
    private ISeries[] _series;
    [ObservableProperty]
    private Axis[] _xAxes;
    [ObservableProperty]
    private Axis[] _yAxes;

    public HistoryViewModel(IMemoryCache cache, IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        FromDate = DateTime.UtcNow.Date.AddDays(-7);
        ToDate = DateTime.UtcNow.Date;
        // Initialize axes for date vs plays
        XAxes =
        [
            new Axis
            {
                Name = "Date",
                // Safely convert OLE Automation date to string, avoid invalid dates
                Labeler = val =>
                {
                    if (double.IsNaN(val) || double.IsInfinity(val))
                        return string.Empty;
                    try
                    {
                        var dt = DateTime.FromOADate(val);
                        return dt.ToString("yyyy-MM-dd");
                    }
                    catch
                    {
                        return string.Empty;
                    }
                }
            }
        ];
        YAxes =
        [
            new Axis { Name = "Plays" }
        ];
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
            var filtered = allHistory
                .Where(h => h.PlayedAt.Date >= FromDate && h.PlayedAt.Date <= ToDate)
                .ToList();

            TotalPlays = filtered.Count;
            FirstPlayDate = filtered.Count != 0 ? filtered.Min(h => h.PlayedAt) : (DateTime?)null;
            LastPlayDate = filtered.Count != 0 ? filtered.Max(h => h.PlayedAt) : (DateTime?)null;
            AverageDuration = filtered.Count != 0
                ? TimeSpan.FromSeconds(filtered.Average(h => h.DurationSeconds))
                : TimeSpan.Zero;
            // Update chart series
            Series =
            [
                new LineSeries<DateTimePoint>
                {
                    Values = filtered
                        .GroupBy(h => h.PlayedAt.Date)
                        .OrderBy(g => g.Key)
                        .Select(g => new DateTimePoint(g.Key, g.Count()))
                        .ToArray()
                }
            ];
        }
        finally
        {
            IsBusy = false;
        }
    }
}
