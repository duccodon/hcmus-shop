using hcmus_shop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace hcmus_shop.Views.Dashboard.Components.Tables
{
    public sealed partial class ProductSummaryTable : UserControl
    {
        public static readonly DependencyProperty TopSoldItemsProperty = DependencyProperty.Register(
            nameof(TopSoldItems),
            typeof(IEnumerable<ProductMetricItem>),
            typeof(ProductSummaryTable),
            new PropertyMetadata(null, OnItemsChanged));

        public static readonly DependencyProperty LowStockItemsProperty = DependencyProperty.Register(
            nameof(LowStockItems),
            typeof(IEnumerable<ProductMetricItem>),
            typeof(ProductSummaryTable),
            new PropertyMetadata(null, OnItemsChanged));

        private readonly List<ProductMetricItem> _topSoldCache = [];
        private readonly List<ProductMetricItem> _lowStockCache = [];

        public ObservableCollection<int> CountOptions { get; } = [5, 6, 7, 8, 9, 10];
        public ObservableCollection<ProductMetricItem> TopSoldDisplayItems { get; } = [];
        public ObservableCollection<ProductMetricItem> LowStockDisplayItems { get; } = [];

        public ProductSummaryTable()
        {
            InitializeComponent();
            CountSelector.SelectedIndex = 0;
        }

        public IEnumerable<ProductMetricItem> TopSoldItems
        {
            get => (IEnumerable<ProductMetricItem>)GetValue(TopSoldItemsProperty);
            set => SetValue(TopSoldItemsProperty, value);
        }

        public IEnumerable<ProductMetricItem> LowStockItems
        {
            get => (IEnumerable<ProductMetricItem>)GetValue(LowStockItemsProperty);
            set => SetValue(LowStockItemsProperty, value);
        }

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProductSummaryTable control)
            {
                control.RefreshDisplayItems();
            }
        }

        private void CountSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshDisplayItems();
        }

        private void RefreshDisplayItems()
        {
            _topSoldCache.Clear();
            _lowStockCache.Clear();

            if (TopSoldItems is not null)
            {
                _topSoldCache.AddRange(TopSoldItems);
            }

            if (LowStockItems is not null)
            {
                _lowStockCache.AddRange(LowStockItems);
            }

            var count = CountSelector?.SelectedItem as int? ?? 5;

            TopSoldDisplayItems.Clear();
            foreach (var item in _topSoldCache.Take(count))
            {
                TopSoldDisplayItems.Add(item);
            }

            LowStockDisplayItems.Clear();
            foreach (var item in _lowStockCache.Take(count))
            {
                LowStockDisplayItems.Add(item);
            }
        }
    }
}
