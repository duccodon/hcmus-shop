using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace hcmus_shop.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private string welcomeMessage = "Welcome Back👋";

        public ObservableCollection<KpiCardItem> KpiCards { get; } = new();
        public ObservableCollection<InvoiceLegendItem> InvoiceLegends { get; } = new();
        public ObservableCollection<RecentInvoiceItem> RecentInvoices { get; } = new();

        public IEnumerable<ISeries> InvoiceSeries { get; }
        public IEnumerable<ISeries> SalesSeries { get; }
        public Axis[] SalesXAxes { get; }
        public Axis[] SalesYAxes { get; }

        public DashboardViewModel()
        {
            KpiCards.Add(new KpiCardItem("Customers", "1,456", "+1.65%", "", true));
            KpiCards.Add(new KpiCardItem("Revenue", "$3,345", "+2.01%", "", true));
            KpiCards.Add(new KpiCardItem("Profit", "60%", "+0.35%", "", true));
            KpiCards.Add(new KpiCardItem("Invoices", "1,135", "-1.52%", "", false));

            InvoiceLegends.Add(new InvoiceLegendItem("Paid", 1135, new SolidColorBrush(ColorHelper.FromArgb(255, 6, 182, 212))));
            InvoiceLegends.Add(new InvoiceLegendItem("Overdue", 234, new SolidColorBrush(ColorHelper.FromArgb(255, 124, 58, 237))));
            InvoiceLegends.Add(new InvoiceLegendItem("Unpaid", 514, new SolidColorBrush(ColorHelper.FromArgb(255, 245, 158, 11))));
            InvoiceLegends.Add(new InvoiceLegendItem("Draft", 345, new SolidColorBrush(ColorHelper.FromArgb(255, 148, 163, 184))));

            RecentInvoices.Add(new RecentInvoiceItem("#054579", "Derry Vengger", "Air Black Backpack", "21/07/2021 08:21", "Paid", "$190", new SolidColorBrush(ColorHelper.FromArgb(255, 22, 163, 74))));
            RecentInvoices.Add(new RecentInvoiceItem("#054589", "Levi Ackermann", "Air Trend Backpack", "21/07/2021 08:21", "Pending", "$244", new SolidColorBrush(ColorHelper.FromArgb(255, 217, 119, 6))));
            RecentInvoices.Add(new RecentInvoiceItem("#054699", "Rikael Brown", "Air Blue Backpack", "21/07/2021 08:21", "Paid", "$121", new SolidColorBrush(ColorHelper.FromArgb(255, 22, 163, 74))));
            RecentInvoices.Add(new RecentInvoiceItem("#054499", "Norton Rivas", "Air Black Backpack", "21/07/2022 09:21", "Canceled", "$300", new SolidColorBrush(ColorHelper.FromArgb(255, 236, 72, 153))));

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

            SalesXAxes =
            [
                new Axis
                {
                    Labels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],
                    LabelsPaint = null
                }
            ];

            SalesYAxes =
            [
                new Axis
                {
                    MinLimit = 0,
                    MaxLimit = 24,
                    Labeler = value => $"{value:0}k"
                }
            ];
        }
    }

    public sealed record KpiCardItem(string Title, string Value, string DeltaText, string Glyph, bool IsPositive);

    public sealed record InvoiceLegendItem(string Label, int Value, Brush MarkerBrush);

    public sealed record RecentInvoiceItem(string No, string Customer, string Product, string Date, string Status, string Price, Brush StatusBrush);
}
