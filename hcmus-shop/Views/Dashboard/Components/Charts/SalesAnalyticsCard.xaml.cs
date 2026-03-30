using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
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
            nameof(XAxes), typeof(IEnumerable<ICartesianAxis>), typeof(SalesAnalyticsCard), new PropertyMetadata(null));

        public static readonly DependencyProperty YAxesProperty = DependencyProperty.Register(
            nameof(YAxes), typeof(IEnumerable<ICartesianAxis>), typeof(SalesAnalyticsCard), new PropertyMetadata(null));

        public SalesAnalyticsCard()
        {
            InitializeComponent();
        }

        public IEnumerable<ISeries> Series
        {
            get => (IEnumerable<ISeries>)GetValue(SeriesProperty);
            set => SetValue(SeriesProperty, value);
        }

        public IEnumerable<ICartesianAxis> XAxes
        {
            get => (IEnumerable<ICartesianAxis>)GetValue(XAxesProperty);
            set => SetValue(XAxesProperty, value);
        }

        public IEnumerable<ICartesianAxis> YAxes
        {
            get => (IEnumerable<ICartesianAxis>)GetValue(YAxesProperty);
            set => SetValue(YAxesProperty, value);
        }
    }
}