using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Models.DTOs;
using hcmus_shop.ViewModels.Orders;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

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
            ViewModel.RequestInvoicePathAsync = RequestInvoicePathAsync;
            ViewModel.ConfirmDeleteOrderAsync = ShowDeleteConfirmAsync;
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
            ViewModel.RequestInvoicePathAsync = null;
            ViewModel.ConfirmDeleteOrderAsync = null;
        }

        private async Task<string?> RequestInvoicePathAsync(string suggestedFileName)
        {
            if ((Application.Current as App)?.CurrentWindow is not Window window)
            {
                return null;
            }

            var picker = new FileSavePicker();
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
            picker.FileTypeChoices.Add("PDF Document", [".pdf"]);
            picker.SuggestedFileName = suggestedFileName;

            var file = await picker.PickSaveFileAsync();
            return file?.Path;
        }

        private async Task<bool> ShowDeleteConfirmAsync(OrderDto order)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Order",
                Content = $"Delete order {order.OrderId}?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
    }
}
