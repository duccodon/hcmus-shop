using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Promotions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace hcmus_shop.Views
{
    public sealed partial class PromotionsPage : Page
    {
        public PromotionsViewModel ViewModel { get; }

        public PromotionsPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<PromotionsViewModel>();
            DataContext = ViewModel;
            ViewModel.RequestPromotionEditorAsync = ShowPromotionEditorAsync;
            ViewModel.ConfirmDeletePromotionAsync = ShowDeletePromotionConfirmAsync;
            Loaded += PromotionsPage_Loaded;
            Unloaded += PromotionsPage_Unloaded;
        }

        private async void PromotionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsInitialized)
            {
                await ViewModel.InitializeCommand.ExecuteAsync(null);
            }
        }

        private void PromotionsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= PromotionsPage_Loaded;
            Unloaded -= PromotionsPage_Unloaded;
            ViewModel.RequestPromotionEditorAsync = null;
            ViewModel.ConfirmDeletePromotionAsync = null;
        }

        private async Task<PromotionEditorResult?> ShowPromotionEditorAsync(PromotionEditorState state)
        {
            var startDate = ClampDateForPicker(state.StartDate);
            var endDate = ClampDateForPicker(state.EndDate);

            var codeBox = new TextBox
            {
                PlaceholderText = "Promotion code",
                Text = state.Code
            };

            var percentBox = new NumberBox
            {
                PlaceholderText = "Percent",
                Value = state.DiscountPercent ?? 0,
                Minimum = 0
            };

            var amountBox = new NumberBox
            {
                PlaceholderText = "Amount",
                Value = state.DiscountAmount ?? 0,
                Minimum = 0
            };

            var startPicker = new DatePicker
            {
                Date = startDate
            };

            var endPicker = new DatePicker
            {
                Date = endDate
            };

            var activeSwitch = new ToggleSwitch
            {
                Header = "Active",
                IsOn = state.IsActive
            };

            var panel = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Code" },
                    codeBox,
                    new TextBlock { Text = "Discount Percent (set one discount field only)" },
                    percentBox,
                    new TextBlock { Text = "Discount Amount" },
                    amountBox,
                    new TextBlock { Text = "Start Date" },
                    startPicker,
                    new TextBlock { Text = "End Date" },
                    endPicker,
                    activeSwitch
                }
            };

            var dialog = new ContentDialog
            {
                Title = state.IsEditMode ? "Edit Promotion" : "Add Promotion",
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

            return new PromotionEditorResult
            {
                Code = codeBox.Text,
                DiscountPercent = percentBox.Value > 0 ? percentBox.Value : null,
                DiscountAmount = amountBox.Value > 0 ? amountBox.Value : null,
                StartDate = startPicker.Date,
                EndDate = endPicker.Date,
                IsActive = activeSwitch.IsOn
            };
        }

        private async Task<bool> ShowDeletePromotionConfirmAsync(PromotionListItemViewModel item)
        {
            var dialog = new ContentDialog
            {
                Title = "Deactivate Promotion",
                Content = $"Deactivate promotion {item.Code}?",
                PrimaryButtonText = "Deactivate",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private static DateTimeOffset ClampDateForPicker(DateTimeOffset value)
        {
            var min = new DateTimeOffset(new DateTime(1900, 1, 1));
            var max = new DateTimeOffset(new DateTime(2100, 12, 31));

            if (value < min)
            {
                return DateTimeOffset.Now;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
