using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Reports.Dto;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Reports
{
    public class ReportsViewModel : ObservableObject
    {
        private readonly IReportService _reportService;
        private bool _isInitialized;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private DateTimeOffset _fromDate = DateTimeOffset.Now.AddMonths(-1);
        private DateTimeOffset _toDate = DateTimeOffset.Now;
        private string _selectedGroupBy = "day";
        private string _lastUpdatedText = "Not updated yet";
        private CancellationTokenSource? _refreshDebounceCts;
        private bool _pendingRefresh;
        private int _selectedMonthNumber = DateTime.Now.Month;
        private int _selectedYear = DateTime.Now.Year;
        private bool _isSynchronizingDateState;

        public ReportsViewModel(IReportService reportService)
        {
            _reportService = reportService;
            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized && !IsLoading);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsLoading);

            GroupByOptions.Add("day");
            GroupByOptions.Add("week");
            GroupByOptions.Add("month");
            GroupByOptions.Add("year");

            foreach (var month in Enumerable.Range(1, 12))
            {
                MonthOptions.Add(new ReportMonthOption(month, CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)));
            }

            foreach (var year in Enumerable.Range(DateTime.Now.Year - 10, 12).Reverse())
            {
                YearOptions.Add(year);
            }
        }

        public ObservableCollection<SalesReportEntryDto> SalesEntries { get; } = [];
        public ObservableCollection<TopProductEntryDto> TopProducts { get; } = [];
        public ObservableCollection<string> GroupByOptions { get; } = [];
        public ObservableCollection<ReportMonthOption> MonthOptions { get; } = [];
        public ObservableCollection<int> YearOptions { get; } = [];
        public ObservableCollection<ISeries> QuantitySeries { get; } = [];
        public ObservableCollection<ISeries> RevenueSeries { get; } = [];
        public ObservableCollection<ISeries> ProfitSeries { get; } = [];
        public ObservableCollection<ISeries> RevenueShareSeries { get; } = [];
        public ObservableCollection<ICartesianAxis> PeriodAxes { get; } = [];
        public ObservableCollection<ICartesianAxis> QuantityAxes { get; } = [];
        public ObservableCollection<ICartesianAxis> CurrencyAxes { get; } = [];

        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }

        public bool IsInitialized
        {
            get => _isInitialized;
            private set
            {
                if (SetProperty(ref _isInitialized, value))
                {
                    InitializeCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    RefreshCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool IsEmpty => !IsLoading && SalesEntries.Count == 0 && !HasError;
        public bool IsDayMode => string.Equals(SelectedGroupBy, "day", StringComparison.OrdinalIgnoreCase);
        public bool IsWeekMode => string.Equals(SelectedGroupBy, "week", StringComparison.OrdinalIgnoreCase);
        public bool IsMonthMode => string.Equals(SelectedGroupBy, "month", StringComparison.OrdinalIgnoreCase);
        public bool IsYearMode => string.Equals(SelectedGroupBy, "year", StringComparison.OrdinalIgnoreCase);
        public string DayRangeDisplay => $"{FromDate:dd MMM yyyy} - {ToDate:dd MMM yyyy}";

        public DateTimeOffset FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                {
                    SyncSelectorsFromDateRange();
                    OnPropertyChanged(nameof(DayRangeDisplay));
                    QueueRefresh();
                }
            }
        }

        public DateTimeOffset ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value))
                {
                    SyncSelectorsFromDateRange();
                    OnPropertyChanged(nameof(DayRangeDisplay));
                    QueueRefresh();
                }
            }
        }

        public string SelectedGroupBy
        {
            get => _selectedGroupBy;
            set
            {
                if (SetProperty(ref _selectedGroupBy, value))
                {
                    OnPropertyChanged(nameof(IsDayMode));
                    OnPropertyChanged(nameof(IsWeekMode));
                    OnPropertyChanged(nameof(IsMonthMode));
                    OnPropertyChanged(nameof(IsYearMode));
                    ApplySelectionModeDefaults();
                }
            }
        }

        public int SelectedMonthNumber
        {
            get => _selectedMonthNumber;
            set
            {
                if (SetProperty(ref _selectedMonthNumber, value) && IsMonthMode && !_isSynchronizingDateState)
                {
                    ApplyMonthSelection();
                }
            }
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value) && !_isSynchronizingDateState)
                {
                    if (IsMonthMode)
                    {
                        ApplyMonthSelection();
                    }
                    else if (IsYearMode)
                    {
                        ApplyYearSelection();
                    }
                }
            }
        }

        public string LastUpdatedText
        {
            get => _lastUpdatedText;
            private set => SetProperty(ref _lastUpdatedText, value);
        }

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            IsInitialized = true;
            ApplySelectionModeDefaults();
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            if (IsLoading)
            {
                _pendingRefresh = true;
                return;
            }

            IsLoading = true;
            _pendingRefresh = false;
            ErrorMessage = string.Empty;

            try
            {
                if (FromDate > ToDate)
                {
                    ErrorMessage = "Start date cannot be later than end date.";
                    return;
                }

                var from = FromDate.ToString("yyyy-MM-dd");
                var to = ToDate.ToString("yyyy-MM-dd");

                var salesTask = _reportService.GetSalesReportAsync(new SalesReportRequest
                {
                    FromDate = from,
                    ToDate = to,
                    GroupBy = SelectedGroupBy
                });
                var topProductsTask = _reportService.GetTopProductsAsync(new TopProductsRequest
                {
                    FromDate = from,
                    ToDate = to,
                    Limit = 5
                });

                await Task.WhenAll(salesTask, topProductsTask);

                if (!salesTask.Result.IsSuccess || salesTask.Result.Value is null)
                {
                    ErrorMessage = salesTask.Result.Error ?? "Failed to load sales report.";
                    return;
                }

                if (!topProductsTask.Result.IsSuccess || topProductsTask.Result.Value is null)
                {
                    ErrorMessage = topProductsTask.Result.Error ?? "Failed to load top products.";
                    return;
                }

                ApplySalesEntries(salesTask.Result.Value);
                ApplyTopProducts(topProductsTask.Result.Value);
                LastUpdatedText = $"Last updated: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            }
            finally
            {
                IsLoading = false;
                if (_pendingRefresh)
                {
                    QueueRefresh();
                }
            }
        }

        private void QueueRefresh()
        {
            if (!IsInitialized)
            {
                return;
            }

            _pendingRefresh = true;
            _refreshDebounceCts?.Cancel();
            _refreshDebounceCts?.Dispose();
            _refreshDebounceCts = new CancellationTokenSource();
            _ = DebouncedRefreshAsync(_refreshDebounceCts.Token);
        }

        private async Task DebouncedRefreshAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(250, token);
                if (!token.IsCancellationRequested && _pendingRefresh)
                {
                    await RefreshAsync();
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void ApplySelectionModeDefaults()
        {
            var today = DateTimeOffset.Now.Date;

            if (IsDayMode)
            {
                SetDateRange(today, today);
                return;
            }

            if (IsWeekMode)
            {
                var weekStart = today.AddDays(-(int)(today.DayOfWeek == DayOfWeek.Sunday ? 6 : today.DayOfWeek - DayOfWeek.Monday));
                SetDateRange(weekStart, today);
                return;
            }

            if (IsMonthMode)
            {
                _isSynchronizingDateState = true;
                SelectedMonthNumber = today.Month;
                SelectedYear = today.Year;
                _isSynchronizingDateState = false;
                ApplyMonthSelection();
                return;
            }

            _isSynchronizingDateState = true;
            SelectedYear = today.Year;
            _isSynchronizingDateState = false;
            ApplyYearSelection();
        }

        private void ApplyMonthSelection()
        {
            var monthStart = new DateTimeOffset(SelectedYear, SelectedMonthNumber, 1, 0, 0, 0, TimeSpan.Zero);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            SetDateRange(monthStart, monthEnd);
        }

        private void ApplyYearSelection()
        {
            var yearStart = new DateTimeOffset(SelectedYear, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var yearEnd = new DateTimeOffset(SelectedYear, 12, 31, 0, 0, 0, TimeSpan.Zero);
            SetDateRange(yearStart, yearEnd);
        }

        private void SyncSelectorsFromDateRange()
        {
            _isSynchronizingDateState = true;
            SelectedMonthNumber = FromDate.Month;
            SelectedYear = FromDate.Year;
            _isSynchronizingDateState = false;
        }

        private void SetDateRange(DateTimeOffset from, DateTimeOffset to)
        {
            _isSynchronizingDateState = true;
            var fromChanged = SetProperty(ref _fromDate, from, nameof(FromDate));
            var toChanged = SetProperty(ref _toDate, to, nameof(ToDate));
            _isSynchronizingDateState = false;

            if (fromChanged || toChanged)
            {
                OnPropertyChanged(nameof(DayRangeDisplay));
                SyncSelectorsFromDateRange();
                QueueRefresh();
            }
        }

        private void ApplySalesEntries(System.Collections.Generic.List<SalesReportEntryDto> entries)
        {
            SalesEntries.Clear();
            foreach (var entry in entries)
            {
                SalesEntries.Add(entry);
            }

            var labels = entries.Select(entry => entry.Period).ToList();

            QuantitySeries.Clear();
            QuantitySeries.Add(new LineSeries<double>
            {
                Values = entries.Select(entry => (double)entry.TotalQuantity).ToArray(),
                Name = "Quantity"
            });

            RevenueSeries.Clear();
            RevenueSeries.Add(new ColumnSeries<double>
            {
                Values = entries.Select(entry => entry.TotalRevenue).ToArray(),
                Name = "Revenue"
            });

            ProfitSeries.Clear();
            ProfitSeries.Add(new ColumnSeries<double>
            {
                Values = entries.Select(entry => entry.TotalProfit).ToArray(),
                Name = "Profit"
            });

            PeriodAxes.Clear();
            PeriodAxes.Add(new Axis { Labels = labels });

            QuantityAxes.Clear();
            QuantityAxes.Add(new Axis
            {
                MinLimit = 0,
                Labeler = value => value.ToString("N0", CultureInfo.InvariantCulture)
            });

            CurrencyAxes.Clear();
            CurrencyAxes.Add(new Axis
            {
                MinLimit = 0,
                Labeler = value => FormatCurrency(value)
            });
        }

        private void ApplyTopProducts(System.Collections.Generic.List<TopProductEntryDto> entries)
        {
            TopProducts.Clear();
            foreach (var entry in entries)
            {
                TopProducts.Add(entry);
            }

            RevenueShareSeries.Clear();
            foreach (var entry in entries)
            {
                RevenueShareSeries.Add(new PieSeries<double>
                {
                    Values = new[] { entry.TotalRevenue },
                    Name = entry.Name,
                    InnerRadius = 30
                });
            }
        }

        public static string FormatCurrency(double value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture) + " VND";
        }
    }

    public class ReportMonthOption
    {
        public ReportMonthOption(int monthNumber, string displayName)
        {
            MonthNumber = monthNumber;
            DisplayName = displayName;
        }

        public int MonthNumber { get; }
        public string DisplayName { get; }
    }
}
