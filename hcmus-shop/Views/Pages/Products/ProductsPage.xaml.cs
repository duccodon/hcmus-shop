using hcmus_shop.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace hcmus_shop.Views
{
    public sealed partial class ProductsPage : Page, INotifyPropertyChanged
    {
        private readonly List<Product> _allProducts = [];
        private List<Product> _filteredProducts = [];
        private readonly HashSet<int> _selectedProductIds = [];

        private int _currentPage = 1;
        private int _selectedPageSize = 10;
        private string _searchQuery = string.Empty;

        public ObservableCollection<Product> PagedProducts { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [10, 20, 50];
        public ObservableCollection<ProductPageButtonItem> PageButtons { get; } = [];

        public event PropertyChangedEventHandler? PropertyChanged;

        public ProductsPage()
        {
            InitializeComponent();
            SeedProducts();
            ApplyFilter();
        }

        public Visibility HasSelection => _selectedProductIds.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        public string SelectionActionText => $"{_selectedProductIds.Count} selected";

        public int SelectedPageSize
        {
            get => _selectedPageSize;
            set
            {
                if (_selectedPageSize == value) return;
                _selectedPageSize = value;
                _currentPage = 1;
                RefreshPaging();
                OnPropertyChanged(nameof(SelectedPageSize));
            }
        }

        public string ResultText
        {
            get
            {
                if (_filteredProducts.Count == 0) return "Result 0 of 0";
                var start = ((_currentPage - 1) * SelectedPageSize) + 1;
                var end = Math.Min(_currentPage * SelectedPageSize, _filteredProducts.Count);
                return $"Result {start}-{end} of {_filteredProducts.Count}";
            }
        }

        private int TotalPages =>
            Math.Max(1, (int)Math.Ceiling((double)_filteredProducts.Count / SelectedPageSize));

        // Search handler
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchBox.Text.Trim().ToLowerInvariant();
            _currentPage = 1;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _filteredProducts = string.IsNullOrEmpty(_searchQuery)
                ? [.. _allProducts]
                : [.. _allProducts.Where(p =>
                    p.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    GetCategoryName(p.Categories).Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.Sku.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))];

            RefreshPaging();
        }

        public bool IsProductSelected(int productId)
        {
            return _selectedProductIds.Contains(productId);
        }

        public static string GetCategoryName(ICollection<Category>? categories)
        {
            return categories?.FirstOrDefault()?.Name ?? "Uncategorized";
        }

        public static string GetStockLabel(int stockQuantity)
        {
            if (stockQuantity <= 0)
            {
                return "Out of Stock";
            }

            return stockQuantity < 10 ? "Low Stock" : string.Empty;
        }

        public static SolidColorBrush GetStockLabelBrush(int stockQuantity)
        {
            if (stockQuantity <= 0)
            {
                return new SolidColorBrush(ColorHelper.FromArgb(255, 220, 38, 38));
            }

            if (stockQuantity < 10)
            {
                return new SolidColorBrush(ColorHelper.FromArgb(255, 202, 138, 4));
            }

            return new SolidColorBrush(Colors.Transparent);
        }

        public static string GetSellingPriceText(decimal sellingPrice)
        {
            return $"${sellingPrice:0}";
        }

        public static string GetStatusText(bool isActive, int stockQuantity)
        {
            return isActive
                ? "Published"
                : stockQuantity <= 0 ? "Stock Out" : "Inactive";
        }

        public static SolidColorBrush GetStatusBackground(bool isActive, int stockQuantity)
        {
            return GetStatusText(isActive, stockQuantity) switch
            {
                "Published" => new SolidColorBrush(ColorHelper.FromArgb(220, 220, 252, 231)),
                "Inactive" => new SolidColorBrush(ColorHelper.FromArgb(220, 254, 226, 226)),
                _ => new SolidColorBrush(ColorHelper.FromArgb(220, 254, 243, 199)),
            };
        }

        public static SolidColorBrush GetStatusForeground(bool isActive, int stockQuantity)
        {
            return GetStatusText(isActive, stockQuantity) switch
            {
                "Published" => new SolidColorBrush(ColorHelper.FromArgb(255, 21, 128, 61)),
                "Inactive" => new SolidColorBrush(ColorHelper.FromArgb(255, 185, 28, 28)),
                _ => new SolidColorBrush(ColorHelper.FromArgb(255, 161, 98, 7)),
            };
        }

        private void PageSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageSizeCombo.SelectedItem is int size)
            {
                SelectedPageSize = size;
            }
        }

        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: int page }) return;
            if (page < 1 || page > TotalPages || page == _currentPage) return;
            _currentPage = page;
            RefreshPaging();
        }

        private void RefreshPaging()
        {
            if (_currentPage > TotalPages)
            {
                _currentPage = TotalPages;
            }

            var pageItems = _filteredProducts
                .Skip((_currentPage - 1) * SelectedPageSize)
                .Take(SelectedPageSize)
                .ToList();

            PagedProducts.Clear();
            foreach (var item in pageItems)
                PagedProducts.Add(item);

            RebuildPageButtons();
            UpdateSelectAllState();
            OnPropertyChanged(nameof(ResultText));
        }

        private void RebuildPageButtons()
        {
            PageButtons.Clear();

            var accentBackground = new SolidColorBrush(ColorHelper.FromArgb(255, 111, 126, 255));
            var normalBackground = new SolidColorBrush(Colors.Transparent);
            var activeForeground = new SolidColorBrush(Colors.White);
            var normalForeground = Application.Current.Resources["TextPrimary"] is SolidColorBrush brush
                ? brush
                : new SolidColorBrush(Colors.Black);

            ProductPageButtonItem ButtonItem(string label, int pageNumber, bool enabled, bool active = false) => new()
            {
                Label = label,
                PageNumber = pageNumber,
                IsEnabled = enabled,
                Background = active ? accentBackground : normalBackground,
                Foreground = active ? activeForeground : normalForeground,
            };

            PageButtons.Add(ButtonItem("← Previous", _currentPage - 1, _currentPage > 1));

            var pages = BuildPageNumbers(_currentPage, TotalPages);
            int? previousPage = null;

            foreach (var page in pages)
            {
                if (previousPage.HasValue && page - previousPage.Value > 1)
                    PageButtons.Add(ButtonItem("…", -1, false));

                PageButtons.Add(ButtonItem(
                    page.ToString(),
                    page,
                    enabled: page != _currentPage,
                    active: page == _currentPage));

                previousPage = page;
            }

            PageButtons.Add(ButtonItem("Next →", _currentPage + 1, _currentPage < TotalPages));
        }

        private static IEnumerable<int> BuildPageNumbers(int currentPage, int totalPages)
        {
            var pages = new SortedSet<int> { 1, totalPages };
            for (var offset = -1; offset <= 1; offset++)
            {
                var value = currentPage + offset;
                if (value >= 1 && value <= totalPages)
                    pages.Add(value);
            }
            return pages;
        }

        private void SeedProducts()
        {
            var id = 1;

            _allProducts.AddRange(
            [
                CreateProduct(id++, "SUN-001", "Casual Sunglass", "Accessories",  124, 47m,  true),
                CreateProduct(id++, "TEE-001", "T-Shirt",          "Clothes",      124, 47m,  true),
                CreateProduct(id++, "TEA-001", "Green Tea",         "Beauty",         0, 47m,  false),
                CreateProduct(id++, "DEN-001", "Denim Shirt",       "Clothes",      124, 47m,  false),
                CreateProduct(id++, "JCK-001", "Casual Jacket",     "Clothes",        0, 47m,  false),
                CreateProduct(id++, "CAP-001", "Cap",               "Accessories",  124, 47m,  true),
                CreateProduct(id++, "SHO-001", "Nike Cats",         "Shoes",        124, 47m,  false),
                CreateProduct(id++, "FAN-001", "Cooling Fan",       "Electronics",  124, 47m,  false),
                CreateProduct(id++, "WAT-001", "Man Watch",         "Accessories",  124, 47m,  false),
                CreateProduct(id++, "LAP-001", "Laptop Stand",      "Accessories",   46, 52m,  true),
                CreateProduct(id++, "MOU-001", "Gaming Mouse",      "Electronics",    6, 31m,  true),
                CreateProduct(id++, "KEY-001", "Mechanical Keyboard","Electronics",   2, 69m,  true),
                CreateProduct(id++, "BAG-001", "Crossbody Bag",     "Accessories",    0, 39m,  false),
                CreateProduct(id++, "PWR-001", "Power Bank",        "Electronics",   76, 29m,  true),
            ]);
        }

        private static Product CreateProduct(
            int productId,
            string sku,
            string name,
            string category,
            int stock,
            decimal sellingPrice,
            bool isActive)
        {
            return new Product
            {
                ProductId = productId,
                Sku = sku,
                Name = name,
                BrandId = 1,
                StockQuantity = stock,
                ImportPrice = Math.Max(1, sellingPrice - 10),
                SellingPrice = sellingPrice,
                IsActive = isActive,
                Categories = [new Category { CategoryId = productId, Name = category }],
            };
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SelectAllCheckBox.IsChecked == true)
            {
                foreach (var product in PagedProducts)
                {
                    _selectedProductIds.Add(product.ProductId);
                }
            }
            else
            {
                foreach (var product in PagedProducts)
                {
                    _selectedProductIds.Remove(product.ProductId);
                }
            }

            NotifySelectionChanged();
            RefreshPaging();
        }

        private void RowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { Tag: int productId })
            {
                return;
            }

            _selectedProductIds.Add(productId);
            NotifySelectionChanged();
            UpdateSelectAllState();
        }

        private void RowCheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { Tag: int productId } checkBox)
            {
                return;
            }

            checkBox.IsChecked = _selectedProductIds.Contains(productId);
        }

        private void RowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox { Tag: int productId })
            {
                return;
            }

            _selectedProductIds.Remove(productId);
            NotifySelectionChanged();
            UpdateSelectAllState();
        }

        private void BulkToggleStatusButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var product in _allProducts.Where(p => _selectedProductIds.Contains(p.ProductId)))
            {
                product.IsActive = !product.IsActive;
            }

            RefreshPaging();
        }

        private void BulkDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            _allProducts.RemoveAll(product => _selectedProductIds.Contains(product.ProductId));
            _filteredProducts.RemoveAll(product => _selectedProductIds.Contains(product.ProductId));
            _selectedProductIds.Clear();

            NotifySelectionChanged();
            RefreshPaging();
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

        private void UpdateSelectAllState()
        {
            if (SelectAllCheckBox is null)
            {
                return;
            }

            if (PagedProducts.Count == 0)
            {
                SelectAllCheckBox.IsChecked = false;
                return;
            }

            var selectedCount = PagedProducts.Count(product => _selectedProductIds.Contains(product.ProductId));
            SelectAllCheckBox.IsChecked = selectedCount == PagedProducts.Count
                ? true
                : selectedCount == 0 ? false : null;
        }

        private void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectionActionText));
        }

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ProductPageButtonItem
    {
        public string Label { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public bool IsEnabled { get; set; }
        public SolidColorBrush? Background { get; set; }
        public SolidColorBrush? Foreground { get; set; }
    }
}
