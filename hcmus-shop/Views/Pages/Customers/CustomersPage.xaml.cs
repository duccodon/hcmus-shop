using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Models.DTOs;
using hcmus_shop.ViewModels.Customers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace hcmus_shop.Views
{
    public sealed partial class CustomersPage : Page
    {
        public CustomersViewModel ViewModel { get; }

        public CustomersPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<CustomersViewModel>();
            DataContext = this;
            ViewModel.RequestCustomerEditorAsync = ShowCustomerEditorAsync;
            ViewModel.ConfirmDeleteCustomerAsync = ShowDeleteConfirmAsync;
            ViewModel.NavigateToCustomerRequested = NavigateToCustomerDetail;
            Loaded += CustomersPage_Loaded;
            Unloaded += CustomersPage_Unloaded;
        }

        private async void CustomersPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsInitialized)
            {
                await ViewModel.InitializeCommand.ExecuteAsync(null);
            }
        }

        private void CustomersPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= CustomersPage_Loaded;
            Unloaded -= CustomersPage_Unloaded;
            ViewModel.RequestCustomerEditorAsync = null;
            ViewModel.ConfirmDeleteCustomerAsync = null;
            ViewModel.NavigateToCustomerRequested = null;
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

        private void NavigateToCustomerDetail(string customerId)
        {
            Frame?.Navigate(typeof(CustomerDetailPage), customerId);
        }

        private async Task<bool> ShowDeleteConfirmAsync(CustomerDto customer)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Customer",
                Content = $"Delete customer {customer.Name}?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
    }
}
