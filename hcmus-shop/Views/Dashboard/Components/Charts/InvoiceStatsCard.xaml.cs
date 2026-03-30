using LiveChartsCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace hcmus_shop.Views.Dashboard.Components.Charts
{
    public sealed partial class InvoiceStatsCard : UserControl
    {
        public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
            nameof(Series), typeof(IEnumerable<ISeries>), typeof(InvoiceStatsCard), new PropertyMetadata(null));

        public static readonly DependencyProperty LegendsProperty = DependencyProperty.Register(
            nameof(Legends), typeof(IEnumerable<object>), typeof(InvoiceStatsCard), new PropertyMetadata(null));

        public InvoiceStatsCard()
        {
            InitializeComponent();
        }

        public IEnumerable<ISeries> Series
        {
            get => (IEnumerable<ISeries>)GetValue(SeriesProperty);
            set => SetValue(SeriesProperty, value);
        }

        public IEnumerable<object> Legends
        {
            get => (IEnumerable<object>)GetValue(LegendsProperty);
            set => SetValue(LegendsProperty, value);
        }
    }
}
