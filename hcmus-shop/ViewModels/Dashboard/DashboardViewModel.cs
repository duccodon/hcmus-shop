using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace hcmus_shop.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private string welcomeMessage = "Welcome Back 👋";

        public ObservableCollection<KpiCardItem> KpiCards { get; } = new();
        public ObservableCollection<InvoiceLegendItem> InvoiceLegends { get; } = new();
        public ObservableCollection<RecentInvoiceItem> RecentInvoices { get; } = new();
        public ObservableCollection<ProductMetricItem> TopSoldProducts { get; } = new();
        public ObservableCollection<ProductMetricItem> LowStockProducts { get; } = new();

        public IEnumerable<ISeries> InvoiceSeries { get; }
        public IEnumerable<ISeries> SalesSeries { get; }
        public IEnumerable<ICartesianAxis> SalesXAxes { get; }
        public IEnumerable<ICartesianAxis> SalesYAxes { get; }

        public DashboardViewModel()
        {
            // KPI Cards
            KpiCards.Add(new KpiCardItem
            {
                Title = "Customers",
                Value = "1,456",
                DeltaText = "+1.65%",
                Glyph = "",
                IsPositive = true
            });

            KpiCards.Add(new KpiCardItem
            {
                Title = "Revenue",
                Value = "$3,345",
                DeltaText = "+2.01%",
                Glyph = "",
                IsPositive = true
            });

            KpiCards.Add(new KpiCardItem
            {
                Title = "Profit",
                Value = "60%",
                DeltaText = "+0.35%",
                Glyph = "",
                IsPositive = true
            });

            KpiCards.Add(new KpiCardItem
            {
                Title = "Invoices",
                Value = "1,135",
                DeltaText = "-1.52%",
                Glyph = "",
                IsPositive = false
            });

            // Invoice Legends
            InvoiceLegends.Add(new InvoiceLegendItem
            {
                Label = "Paid",
                Value = 1135,
            });

            InvoiceLegends.Add(new InvoiceLegendItem
            {
                Label = "Overdue",
                Value = 234,
            });

            InvoiceLegends.Add(new InvoiceLegendItem
            {
                Label = "Unpaid",
                Value = 514,
            });

            InvoiceLegends.Add(new InvoiceLegendItem
            {
                Label = "Draft",
                Value = 345,
            });

            // Recent Invoices
            RecentInvoices.Add(new RecentInvoiceItem
            {
                No = "#054579",
                Customer = "Derry Vengger",
                Product = "Air Black Backpack",
                Date = "21/07/2021 08:21",
                Status = "Paid",
                Price = "$190"
            });

            RecentInvoices.Add(new RecentInvoiceItem
            {
                No = "#054589",
                Customer = "Levi Ackermann",
                Product = "Air Trend Backpack",
                Date = "21/07/2021 08:21",
                Status = "Pending",
                Price = "$244"
            });

            RecentInvoices.Add(new RecentInvoiceItem
            {
                No = "#054699",
                Customer = "Rikael Brown",
                Product = "Air Blue Backpack",
                Date = "21/07/2021 08:21",
                Status = "Paid",
                Price = "$121"
            });

            RecentInvoices.Add(new RecentInvoiceItem
            {
                No = "#054499",
                Customer = "Norton Rivas",
                Product = "Air Black Backpack",
                Date = "21/07/2022 09:21",
                Status = "Cancelled",
                Price = "$300"
            });

            RecentInvoices.Add(new RecentInvoiceItem
            {
                No = "#054733",
                Customer = "Anna Moritz",
                Product = "Urban Laptop Sleeve",
                Date = "22/07/2022 11:05",
                Status = "Paid",
                Price = "$88"
            });

            RecentInvoices.Add(new RecentInvoiceItem
            {
                No = "#054810",
                Customer = "Kai Morel",
                Product = "Travel Cable Pouch",
                Date = "23/07/2022 17:42",
                Status = "Pending",
                Price = "$56"
            });

            RecentInvoices.Add(new RecentInvoiceItem
            {
                No = "#054901",
                Customer = "Sena Walsh",
                Product = "Classic Tote",
                Date = "24/07/2022 09:08",
                Status = "Paid",
                Price = "$132"
            });

            // Top sold products
            TopSoldProducts.Add(new ProductMetricItem { Product = "Air Black Backpack", Value = "1,320" });
            TopSoldProducts.Add(new ProductMetricItem { Product = "Air Trend Backpack", Value = "1,145" });
            TopSoldProducts.Add(new ProductMetricItem { Product = "Air Blue Backpack", Value = "982" });
            TopSoldProducts.Add(new ProductMetricItem { Product = "Urban Laptop Sleeve", Value = "864" });
            TopSoldProducts.Add(new ProductMetricItem { Product = "Canvas Travel Pack", Value = "790" });
            TopSoldProducts.Add(new ProductMetricItem { Product = "Travel Cable Pouch", Value = "712" });
            TopSoldProducts.Add(new ProductMetricItem { Product = "Classic Tote", Value = "701" });
            TopSoldProducts.Add(new ProductMetricItem { Product = "Slim Card Holder", Value = "655" });
            TopSoldProducts.Add(new ProductMetricItem { Product = "Minimal Sleeve", Value = "642" });
            TopSoldProducts.Add(new ProductMetricItem { Product = "Desk Organizer", Value = "611" });

            // Top products going to run out
            LowStockProducts.Add(new ProductMetricItem { Product = "Air Black Backpack", Value = "4" });
            LowStockProducts.Add(new ProductMetricItem { Product = "Urban Laptop Sleeve", Value = "6" });
            LowStockProducts.Add(new ProductMetricItem { Product = "Slim Card Holder", Value = "8" });
            LowStockProducts.Add(new ProductMetricItem { Product = "Travel Cable Pouch", Value = "9" });
            LowStockProducts.Add(new ProductMetricItem { Product = "Classic Tote", Value = "10" });
            LowStockProducts.Add(new ProductMetricItem { Product = "Minimal Sleeve", Value = "11" });
            LowStockProducts.Add(new ProductMetricItem { Product = "Desk Organizer", Value = "12" });
            LowStockProducts.Add(new ProductMetricItem { Product = "Leather Mouse Pad", Value = "13" });
            LowStockProducts.Add(new ProductMetricItem { Product = "Travel Adapter Kit", Value = "14" });
            LowStockProducts.Add(new ProductMetricItem { Product = "Office Tote", Value = "15" });

            // LiveCharts Series
            InvoiceSeries = new ISeries[]
            {
                new PieSeries<double> { Values = new[] { 1135d }, Name = "Paid", InnerRadius = 45 },
                new PieSeries<double> { Values = new[] { 234d }, Name = "Overdue", InnerRadius = 45 },
                new PieSeries<double> { Values = new[] { 514d }, Name = "Unpaid", InnerRadius = 45 },
                new PieSeries<double> { Values = new[] { 345d }, Name = "Draft", InnerRadius = 45 }
            };

            SalesSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Revenue",
                    Values = new[] { 6d, 14d, 8d, 10d, 16d, 12d, 20d, 11d, 15d, 12d, 13d, 7d },
                    GeometrySize = 8,
                    LineSmoothness = 0.7
                }
            };

            SalesXAxes = new[]
            {
                new Axis
                {
                    Labels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
                }
            };

            SalesYAxes = new[]
            {
                new Axis
                {
                    MinLimit = 0,
                    MaxLimit = 24,
                    Labeler = value => $"{value:0}k"
                }
            };
        }
    }

    // ==================== FIXED CLASSES (Mutable) ====================

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