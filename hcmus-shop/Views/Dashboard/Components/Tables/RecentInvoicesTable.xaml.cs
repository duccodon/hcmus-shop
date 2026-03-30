using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using hcmus_shop.ViewModels;

namespace hcmus_shop.Views.Dashboard.Components.Tables
{
    public sealed partial class RecentInvoicesTable : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable<RecentInvoiceItem>), typeof(RecentInvoicesTable), new PropertyMetadata(null));

        public RecentInvoicesTable()
        {
            InitializeComponent();
        }

        public IEnumerable<RecentInvoiceItem> ItemsSource
        {
            get => (IEnumerable<RecentInvoiceItem>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
    }
}