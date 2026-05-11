using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Models.DTOs;
using hcmus_shop.ViewModels.Customers;
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
            ViewModel.RequestCustomerEditorAsync = ShowCustomerEditorAsync;
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
            _ = ViewModel.PersistDraftAsync();
            ViewModel.RequestInvoicePathAsync = null;
            ViewModel.ConfirmDeleteOrderAsync = null;
            ViewModel.RequestCustomerEditorAsync = null;
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

        private async Task<CustomerEditorResult?> ShowCustomerEditorAsync(CustomerEditorState state)
        {
            var nameBox = new TextBox { Text = state.Name, PlaceholderText = "Customer name" };
            var phoneBox = new TextBox { Text = state.Phone ?? string.Empty, PlaceholderText = "Phone" };
            var emailBox = new TextBox { Text = state.Email ?? string.Empty, PlaceholderText = "Email" };

            var panel = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Name" },
                    nameBox,
                    new TextBlock { Text = "Phone" },
                    phoneBox,
                    new TextBlock { Text = "Email" },
                    emailBox
                }
            };

            var dialog = new ContentDialog
            {
                Title = state.IsEditMode ? "Edit Customer" : "Add Customer",
                Content = panel,
                PrimaryButtonText = state.IsEditMode ? "Save" : "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return null;
            }

            return new CustomerEditorResult
            {
                Name = nameBox.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(phoneBox.Text) ? null : phoneBox.Text.Trim(),
                Email = string.IsNullOrWhiteSpace(emailBox.Text) ? null : emailBox.Text.Trim()
            };
        }

        private void InstanceSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
        }

        private void InstanceSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is OrderProductSuggestionViewModel suggestion)
            {
                sender.Text = suggestion.DisplayText;
            }
        }

        private void InstanceSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is OrderProductSuggestionViewModel suggestion)
            {
                ViewModel.ChooseSuggestedProduct(suggestion);
                sender.Text = string.Empty;
                return;
            }

            if (ViewModel.ProductSuggestions.Count > 0)
            {
                ViewModel.ChooseSuggestedProduct(ViewModel.ProductSuggestions[0]);
                sender.Text = string.Empty;
            }
        }
    }
}
