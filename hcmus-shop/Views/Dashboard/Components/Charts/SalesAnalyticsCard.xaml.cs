using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace hcmus_shop.Views.Dashboard.Components.Charts
{
    public sealed partial class SalesAnalyticsCard : UserControl
    {
        public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
            nameof(Series), typeof(IEnumerable<ISeries>), typeof(SalesAnalyticsCard), new PropertyMetadata(null));

        public static readonly DependencyProperty XAxesProperty = DependencyProperty.Register(
            nameof(XAxes), typeof(IEnumerable<Axis>), typeof(SalesAnalyticsCard), new PropertyMetadata(null));

        public static readonly DependencyProperty YAxesProperty = DependencyProperty.Register(
            nameof(YAxes), typeof(IEnumerable<Axis>), typeof(SalesAnalyticsCard), new PropertyMetadata(null));

        public SalesAnalyticsCard()
        {
            InitializeComponent();
        }

        public IEnumerable<ISeries> Series
        {
            get => (IEnumerable<ISeries>)GetValue(SeriesProperty);
            set => SetValue(SeriesProperty, value);
        }

        public IEnumerable<Axis> XAxes
        {
            get => (IEnumerable<Axis>)GetValue(XAxesProperty);
            set => SetValue(XAxesProperty, value);
        }

        public IEnumerable<Axis> YAxes
        {
            get => (IEnumerable<Axis>)GetValue(YAxesProperty);
            set => SetValue(YAxesProperty, value);
        }
    }
}
