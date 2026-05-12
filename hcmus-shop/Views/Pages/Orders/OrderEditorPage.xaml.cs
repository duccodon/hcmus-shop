using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Customers;
using hcmus_shop.ViewModels.Orders;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace hcmus_shop.Views
{
    public sealed partial class OrderEditorPage : Page
    {
        public OrdersViewModel ViewModel { get; }
        private string? _editingOrderId;

        public OrderEditorPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<OrdersViewModel>();
            DataContext = this;
            ViewModel.RequestCustomerEditorAsync = ShowCustomerEditorAsync;
            ViewModel.OrderSaved += ViewModel_OrderSaved;
            Unloaded += OrderEditorPage_Unloaded;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string orderId)
            {
                _editingOrderId = orderId;
                await ViewModel.PrepareEditAsync(orderId);
            }
            else
            {
                _editingOrderId = null;
                await ViewModel.PrepareCreateAsync();
            }
        }

        private void OrderEditorPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OrderEditorPage_Unloaded;
            _ = ViewModel.PersistDraftAsync();
            ViewModel.RequestCustomerEditorAsync = null;
            ViewModel.OrderSaved -= ViewModel_OrderSaved;
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
                return;
            }

            Frame?.Navigate(typeof(OrdersPage));
        }

        private void ViewModel_OrderSaved(object? sender, System.EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_editingOrderId))
            {
                Frame?.Navigate(typeof(OrderDetailPage), _editingOrderId);
                return;
            }

            Frame?.Navigate(typeof(OrdersPage));
        }

        private void InstanceSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                sender.IsSuggestionListOpen = true;
            }
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
                sender.IsSuggestionListOpen = false;
                return;
            }

            if (ViewModel.ProductSuggestions.Count > 0)
            {
                ViewModel.ChooseSuggestedProduct(ViewModel.ProductSuggestions[0]);
                sender.Text = string.Empty;
                sender.IsSuggestionListOpen = false;
            }
        }

        private void InstanceSuggestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox suggestBox && ViewModel.ProductSuggestions.Count > 0)
            {
                suggestBox.IsSuggestionListOpen = true;
            }
        }

        private void InstanceSuggestBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is AutoSuggestBox suggestBox)
            {
                suggestBox.IsSuggestionListOpen = false;
            }
        }

        private void Root_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source)
            {
                InstanceSuggestBox.IsSuggestionListOpen = false;
                return;
            }

            if (!IsDescendantOf(source, InstanceSuggestBox))
            {
                InstanceSuggestBox.IsSuggestionListOpen = false;
            }
        }

        private static bool IsDescendantOf(DependencyObject? source, DependencyObject ancestor)
        {
            var current = source;
            while (current is not null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }
    }
}
