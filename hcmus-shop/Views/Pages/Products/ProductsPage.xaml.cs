using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Products;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace hcmus_shop.Views
{
    public sealed partial class ProductsPage : Page
    {
        public ProductsViewModel ViewModel { get; }

        public ProductsPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<ProductsViewModel>();
            DataContext = ViewModel;
            ViewModel.NavigateToAddProductRequested += ViewModel_NavigateToAddProductRequested;
            Loaded += ProductsPage_Loaded;
            Unloaded += ProductsPage_Unloaded;
        }

        private async void ProductsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsInitialized)
            {
                await ViewModel.InitializeCommand.ExecuteAsync(null);
            }
        }

        private void ProductsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ProductsPage_Loaded;
            Unloaded -= ProductsPage_Unloaded;
            ViewModel.NavigateToAddProductRequested -= ViewModel_NavigateToAddProductRequested;
        }

        private void ViewModel_NavigateToAddProductRequested(object? sender, EventArgs e)
        {
            Frame?.Navigate(typeof(AddProductPage));
        }

        private void DateFromPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            UpdateDateRangeLabel();
        }

        private void DateToPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            UpdateDateRangeLabel();
        }

        private void UpdateDateRangeLabel()
        {
            var from = DateFromPicker.Date?.Date;
            var to = DateToPicker.Date?.Date;

            if (from is null && to is null)
            {
                DateRangeLabel.Text = "Date Range";
                return;
            }

            var fromText = from?.ToString("dd MMM") ?? "...";
            var toText = to?.ToString("dd MMM yyyy") ?? "...";
            DateRangeLabel.Text = $"{fromText} - {toText}";
        }
    }
}
