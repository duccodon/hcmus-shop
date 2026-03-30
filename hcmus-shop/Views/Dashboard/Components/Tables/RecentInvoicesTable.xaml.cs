using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace hcmus_shop.Views.Dashboard.Components.Tables
{
    public sealed partial class RecentInvoicesTable : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable<object>), typeof(RecentInvoicesTable), new PropertyMetadata(null));

        public RecentInvoicesTable()
        {
            InitializeComponent();
        }

        public IEnumerable<object> ItemsSource
        {
            get => (IEnumerable<object>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
    }
}
