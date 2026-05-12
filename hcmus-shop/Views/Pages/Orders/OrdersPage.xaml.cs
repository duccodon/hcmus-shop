using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Models.DTOs;
using hcmus_shop.ViewModels.Orders;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views
{
    public sealed partial class OrdersPage : Page
    {
        public OrdersViewModel ViewModel { get; }

        public OrdersPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<OrdersViewModel>();
            DataContext = this;
            Loaded += OrdersPage_Loaded;
            Unloaded += OrdersPage_Unloaded;
        }

        private async void OrdersPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsInitialized)
            {
                await ViewModel.InitializeCommand.ExecuteAsync(null);
            }
        }

        private void OrdersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OrdersPage_Loaded;
            Unloaded -= OrdersPage_Unloaded;
        }

        private void CreateOrderButton_Click(object sender, RoutedEventArgs e)
        {
            Frame?.Navigate(typeof(OrderEditorPage));
        }

        private void OrdersListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is OrderDto order)
            {
                Frame?.Navigate(typeof(OrderDetailPage), order.OrderId);
            }
        }
    }
}
