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

            var minimumRankBox = new ComboBox
            {
                ItemsSource = ViewModel.RankOptions,
                SelectedItem = state.MinimumCustomerRank
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

            var validationText = new TextBlock
            {
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DangerForeground"],
                TextWrapping = TextWrapping.Wrap,
                Visibility = Visibility.Collapsed
            };

            var panel = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    validationText,
                    new TextBlock { Text = "Code" },
                    codeBox,
                    new TextBlock { Text = "Discount Percent (set one discount field only)" },
                    percentBox,
                    new TextBlock { Text = "Discount Amount" },
                    amountBox,
                    new TextBlock { Text = "Eligible Customer Rank" },
                    minimumRankBox,
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

            dialog.PrimaryButtonClick += (_, args) =>
            {
                var draft = BuildPromotionEditorResult(
                    codeBox,
                    percentBox,
                    amountBox,
                    minimumRankBox,
                    startPicker,
                    endPicker,
                    activeSwitch);

                if (TryValidateEditorInput(draft, out var validationError))
                {
                    validationText.Text = string.Empty;
                    validationText.Visibility = Visibility.Collapsed;
                    return;
                }

                args.Cancel = true;
                validationText.Text = validationError;
                validationText.Visibility = Visibility.Visible;
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return null;
            }

            return BuildPromotionEditorResult(
                codeBox,
                percentBox,
                amountBox,
                minimumRankBox,
                startPicker,
                endPicker,
                activeSwitch);
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

        private static PromotionEditorResult BuildPromotionEditorResult(
            TextBox codeBox,
            NumberBox percentBox,
            NumberBox amountBox,
            ComboBox minimumRankBox,
            DatePicker startPicker,
            DatePicker endPicker,
            ToggleSwitch activeSwitch)
        {
            return new PromotionEditorResult
            {
                Code = codeBox.Text,
                DiscountPercent = percentBox.Value > 0 ? percentBox.Value : null,
                DiscountAmount = amountBox.Value > 0 ? amountBox.Value : null,
                MinimumCustomerRank = minimumRankBox.SelectedItem as string,
                StartDate = startPicker.Date,
                EndDate = endPicker.Date,
                IsActive = activeSwitch.IsOn
            };
        }

        private static bool TryValidateEditorInput(PromotionEditorResult input, out string message)
        {
            if (string.IsNullOrWhiteSpace(input.Code))
            {
                message = "Promotion code is required.";
                return false;
            }

            if (input.StartDate > input.EndDate)
            {
                message = "Start date must be before or equal to end date.";
                return false;
            }

            var hasPercent = input.DiscountPercent.HasValue && input.DiscountPercent.Value > 0;
            var hasAmount = input.DiscountAmount.HasValue && input.DiscountAmount.Value > 0;
            if (hasPercent == hasAmount)
            {
                message = "Provide either discount percent or discount amount.";
                return false;
            }

            if (hasPercent && input.DiscountPercent!.Value > 100)
            {
                message = "Discount percent must be between 0 and 100.";
                return false;
            }

            message = string.Empty;
            return true;
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
