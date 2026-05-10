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

        public ReportsViewModel(IReportService reportService)
        {
            _reportService = reportService;
            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized && !IsLoading);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsLoading);

            GroupByOptions.Add("day");
            GroupByOptions.Add("week");
            GroupByOptions.Add("month");
            GroupByOptions.Add("year");
        }

        public ObservableCollection<SalesReportEntryDto> SalesEntries { get; } = [];
        public ObservableCollection<TopProductEntryDto> TopProducts { get; } = [];
        public ObservableCollection<string> GroupByOptions { get; } = [];
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

        public DateTimeOffset FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        public DateTimeOffset ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        public string SelectedGroupBy
        {
            get => _selectedGroupBy;
            set => SetProperty(ref _selectedGroupBy, value);
        }

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            await RefreshAsync();
            IsInitialized = true;
        }

        private async Task RefreshAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
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
            }
            finally
            {
                IsLoading = false;
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
}
