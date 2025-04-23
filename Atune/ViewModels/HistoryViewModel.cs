using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Caching.Memory;
using Atune.Data.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Atune.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;

    public HistoryViewModel(IMemoryCache cache, IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        FromDate = DateTime.UtcNow.Date.AddDays(-7);
        ToDate = DateTime.UtcNow.Date;
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
            FirstPlayDate = filtered.Any() ? filtered.Min(h => h.PlayedAt) : (DateTime?)null;
            LastPlayDate = filtered.Any() ? filtered.Max(h => h.PlayedAt) : (DateTime?)null;
            AverageDuration = filtered.Any()
                ? TimeSpan.FromSeconds(filtered.Average(h => h.DurationSeconds))
                : TimeSpan.Zero;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
