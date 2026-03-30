using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
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
                MarkerBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 6, 182, 212))
            });

            InvoiceLegends.Add(new InvoiceLegendItem
            {
                Label = "Overdue",
                Value = 234,
                MarkerBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 124, 58, 237))
            });

            InvoiceLegends.Add(new InvoiceLegendItem
            {
                Label = "Unpaid",
                Value = 514,
                MarkerBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 245, 158, 11))
            });

            InvoiceLegends.Add(new InvoiceLegendItem
            {
                Label = "Draft",
                Value = 345,
                MarkerBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 148, 163, 184))
            });

            // Recent Invoices
            RecentInvoices.Add(new RecentInvoiceItem
            {
                No = "#054579",
                Customer = "Derry Vengger",
                Product = "Air Black Backpack",
                Date = "21/07/2021 08:21",
                Status = "Paid",
                Price = "$190",
                StatusBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 22, 163, 74))
            });

            RecentInvoices.Add(new RecentInvoiceItem
            {
                No = "#054589",
                Customer = "Levi Ackermann",
                Product = "Air Trend Backpack",
                Date = "21/07/2021 08:21",
                Status = "Pending",
                Price = "$244",
                StatusBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 217, 119, 6))
            });

            RecentInvoices.Add(new RecentInvoiceItem
            {
                No = "#054699",
                Customer = "Rikael Brown",
                Product = "Air Blue Backpack",
                Date = "21/07/2021 08:21",
                Status = "Paid",
                Price = "$121",
                StatusBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 22, 163, 74))
            });

            RecentInvoices.Add(new RecentInvoiceItem
            {
                No = "#054499",
                Customer = "Norton Rivas",
                Product = "Air Black Backpack",
                Date = "21/07/2022 09:21",
                Status = "Canceled",
                Price = "$300",
                StatusBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 236, 72, 153))
            });

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
        public Brush MarkerBrush { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }

    public class RecentInvoiceItem
    {
        public string No { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public Brush StatusBrush { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }
}