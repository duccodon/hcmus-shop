using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels
{
    /// <summary>
    /// Dashboard view model. Fetches all KPIs in a single GraphQL query
    /// and maps them to UI collections / chart series.
    /// </summary>
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IDashboardService _dashboardService;

        [ObservableProperty]
        private string _welcomeMessage = "Welcome Back";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        public ObservableCollection<KpiCardItem> KpiCards { get; } = new();
        public ObservableCollection<InvoiceLegendItem> InvoiceLegends { get; } = new();
        public ObservableCollection<RecentInvoiceItem> RecentInvoices { get; } = new();
        public ObservableCollection<ProductMetricItem> TopSoldProducts { get; } = new();
        public ObservableCollection<ProductMetricItem> LowStockProducts { get; } = new();

        public ObservableCollection<ISeries> InvoiceSeries { get; } = new();
        public ObservableCollection<ISeries> SalesSeries { get; } = new();
        public ObservableCollection<ICartesianAxis> SalesXAxes { get; } = new();
        public ObservableCollection<ICartesianAxis> SalesYAxes { get; } = new();

        public DashboardViewModel(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
            // Empty axes so chart renders before first refresh.
            SalesXAxes.Add(new Axis { Labels = new List<string>() });
            SalesYAxes.Add(new Axis
            {
                MinLimit = 0,
                Labeler = value => FormatCurrency(value)
            });
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            ErrorMessage = null;

            var result = await _dashboardService.GetStatsAsync();
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error;
                IsLoading = false;
                return;
            }

            ApplyStats(result.Value!);
            IsLoading = false;
        }

        private void ApplyStats(DashboardStatsDto stats)
        {
            // KPI cards
            KpiCards.Clear();
            KpiCards.Add(new KpiCardItem
            {
                Title = "Total Products",
                Value = stats.TotalProducts.ToString("N0"),
                Glyph = string.Empty,
                IsPositive = true,
            });
            KpiCards.Add(new KpiCardItem
            {
                Title = "Orders Today",
                Value = stats.TotalOrdersToday.ToString("N0"),
                Glyph = string.Empty,
                IsPositive = true,
            });
            KpiCards.Add(new KpiCardItem
            {
                Title = "Revenue Today",
                Value = FormatCurrency(stats.TotalRevenueToday),
                Glyph = string.Empty,
                IsPositive = true,
            });
            KpiCards.Add(new KpiCardItem
            {
                Title = "Low Stock",
                Value = stats.LowStockProducts.Count.ToString("N0"),
                Glyph = string.Empty,
                IsPositive = stats.LowStockProducts.Count == 0,
            });

            // Recent orders
            RecentInvoices.Clear();
            foreach (var order in stats.RecentOrders)
            {
                RecentInvoices.Add(new RecentInvoiceItem
                {
                    No = $"#{ShortId(order.OrderId)}",
                    Customer = order.CustomerName ?? "(walk-in)",
                    Product = string.Empty,
                    Date = FormatDate(order.CreatedAt),
                    Status = order.Status,
                    Price = FormatCurrency(order.FinalAmount),
                });
            }

            // Top selling
            TopSoldProducts.Clear();
            foreach (var p in stats.TopSellingProducts)
            {
                TopSoldProducts.Add(new ProductMetricItem
                {
                    Product = p.Name,
                    Value = p.TotalSold.ToString("N0"),
                });
            }

            // Low stock
            LowStockProducts.Clear();
            foreach (var p in stats.LowStockProducts)
            {
                LowStockProducts.Add(new ProductMetricItem
                {
                    Product = p.Name,
                    Value = p.StockQuantity.ToString(),
                });
            }

            // Invoice status legend (derived from recent orders for now)
            InvoiceLegends.Clear();
            var legendBuckets = stats.RecentOrders
                .GroupBy(o => o.Status)
                .Select(g => new InvoiceLegendItem { Label = g.Key, Value = g.Count() })
                .ToList();
            foreach (var l in legendBuckets) InvoiceLegends.Add(l);

            InvoiceSeries.Clear();
            foreach (var l in legendBuckets)
            {
                InvoiceSeries.Add(new PieSeries<double>
                {
                    Values = new[] { (double)l.Value },
                    Name = l.Label,
                    InnerRadius = 45,
                });
            }

            // Sales line chart — daily revenue this month
            SalesSeries.Clear();
            SalesSeries.Add(new LineSeries<double>
            {
                Name = "Revenue",
                Values = stats.DailyRevenue.Select(d => d.Revenue).ToArray(),
                GeometrySize = 8,
                LineSmoothness = 0.7,
            });

            SalesXAxes.Clear();
            SalesXAxes.Add(new Axis
            {
                Labels = stats.DailyRevenue.Select(d => DayLabel(d.Date)).ToList(),
            });

            SalesYAxes.Clear();
            SalesYAxes.Add(new Axis
            {
                MinLimit = 0,
                Labeler = value => FormatCurrency(value),
            });
        }

        private static string FormatCurrency(double value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture) + " VND";
        }

        private static string FormatDate(string iso)
        {
            return System.DateTime.TryParse(iso, out var dt)
                ? dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : iso;
        }

        private static string DayLabel(string iso)
        {
            if (iso.Length >= 10) return iso.Substring(8, 2);
            return iso;
        }

        private static string ShortId(string id)
        {
            return id.Length <= 8 ? id : id.Substring(0, 8);
        }
    }

    // ==================== UI types (kept stable for existing XAML bindings) ====================

    public class KpiCardItem
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DeltaText { get; set; } = string.Empty;
        public string Glyph { get; set; } = string.Empty;
        public bool IsPositive { get; set; }
    }

    public class InvoiceLegendItem
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class RecentInvoiceItem
    {
        public string No { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ProductMetricItem
    {
        public string Product { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
