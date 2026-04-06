using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace hcmus_shop.ViewModels.Products
{
    public class ProductsViewModel : ObservableObject
    {
        private readonly List<ProductRowViewModel> _allProducts = [];
        private List<ProductRowViewModel> _filteredProducts = [];

        private string _searchQuery = string.Empty;
        private int _selectedPageSize = 10;
        private bool _isAllOnPageSelected;
        private int _currentPage = 1;

        public ProductsViewModel()
        {
            AddProductCommand = new RelayCommand(AddProduct);
            GoToPageCommand = new RelayCommand<int>(GoToPage);
            BulkToggleStatusCommand = new RelayCommand(BulkToggleStatus);
            BulkDeleteCommand = new RelayCommand(BulkDelete);

            SeedProducts();
            ApplyFilter();
        }

        public ObservableCollection<ProductRowViewModel> PagedProducts { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [10, 20, 50];
        public ObservableCollection<PageButtonItem> PageButtons { get; } = [];

        public IRelayCommand AddProductCommand { get; }
        public IRelayCommand<int> GoToPageCommand { get; }
        public IRelayCommand BulkToggleStatusCommand { get; }
        public IRelayCommand BulkDeleteCommand { get; }

        public event EventHandler? NavigateToAddProductRequested;

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _currentPage = 1;
                    ApplyFilter();
                }
            }
        }

        public int SelectedPageSize
        {
            get => _selectedPageSize;
            set
            {
                if (SetProperty(ref _selectedPageSize, value))
                {
                    _currentPage = 1;
                    RefreshPaging();
                    OnPropertyChanged(nameof(ResultText));
                }
            }
        }

        public bool IsAllOnPageSelected
        {
            get => _isAllOnPageSelected;
            set
            {
                if (SetProperty(ref _isAllOnPageSelected, value))
                {
                    foreach (var row in PagedProducts)
                    {
                        row.IsSelected = value;
                    }

                    NotifySelectionChanged();
                }
            }
        }

        public bool HasSelection => _allProducts.Any(p => p.IsSelected);

        public string SelectionActionText => $"{_allProducts.Count(p => p.IsSelected)} selected";

        public string ResultText
        {
            get
            {
                if (_filteredProducts.Count == 0)
                {
                    return "Result 0 of 0";
                }

                var start = ((_currentPage - 1) * SelectedPageSize) + 1;
                var end = Math.Min(_currentPage * SelectedPageSize, _filteredProducts.Count);
                return $"Result {start}-{end} of {_filteredProducts.Count}";
            }
        }

        private int TotalPages =>
            Math.Max(1, (int)Math.Ceiling((double)_filteredProducts.Count / SelectedPageSize));

        private void AddProduct()
        {
            NavigateToAddProductRequested?.Invoke(this, EventArgs.Empty);
        }

        private void GoToPage(int page)
        {
            if (page < 1 || page > TotalPages || page == _currentPage)
            {
                return;
            }

            _currentPage = page;
            RefreshPaging();
            OnPropertyChanged(nameof(ResultText));
        }

        private void BulkToggleStatus()
        {
            foreach (var row in _allProducts.Where(p => p.IsSelected))
            {
                row.IsActive = !row.IsActive;
            }

            NotifySelectionChanged();
            RefreshPaging();
        }

        private void BulkDelete()
        {
            var selectedIds = _allProducts
                .Where(p => p.IsSelected)
                .Select(p => p.ProductId)
                .ToHashSet();

            _allProducts.RemoveAll(product => selectedIds.Contains(product.ProductId));
            _filteredProducts.RemoveAll(product => selectedIds.Contains(product.ProductId));

            NotifySelectionChanged();
            RefreshPaging();
            OnPropertyChanged(nameof(ResultText));
        }

        private void ApplyFilter()
        {
            var query = SearchQuery?.Trim() ?? string.Empty;

            _filteredProducts = string.IsNullOrEmpty(query)
                ? [.. _allProducts]
                :
                [
                    .. _allProducts.Where(p =>
                        p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        p.CategoryDisplay.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        p.Sku.Contains(query, StringComparison.OrdinalIgnoreCase))
                ];

            RefreshPaging();
            OnPropertyChanged(nameof(ResultText));
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
            {
                PagedProducts.Add(item);
            }

            RebuildPageButtons();
            UpdateSelectAllState();
        }

        private void RebuildPageButtons()
        {
            PageButtons.Clear();

            var accentBackground = new SolidColorBrush(ColorHelper.FromArgb(255, 111, 126, 255));
            var normalBackground = new SolidColorBrush(Colors.Transparent);
            var activeForeground = new SolidColorBrush(Colors.White);
            var normalForeground = App.Current.Resources["TextPrimary"] as Brush
                ?? new SolidColorBrush(Colors.Black);

            PageButtons.Add(new PageButtonItem
            {
                Label = "← Previous",
                PageNumber = _currentPage - 1,
                IsEnabled = _currentPage > 1,
                IsCurrent = false,
                Background = normalBackground,
                Foreground = normalForeground,
            });

            var pages = BuildPageNumbers(_currentPage, TotalPages);
            int? previousPage = null;

            foreach (var page in pages)
            {
                if (previousPage.HasValue && page - previousPage.Value > 1)
                {
                    PageButtons.Add(new PageButtonItem
                    {
                        Label = "…",
                        PageNumber = -1,
                        IsEnabled = false,
                        IsCurrent = false,
                        Background = normalBackground,
                        Foreground = normalForeground,
                    });
                }

                PageButtons.Add(new PageButtonItem
                {
                    Label = page.ToString(),
                    PageNumber = page,
                    IsEnabled = page != _currentPage,
                    IsCurrent = page == _currentPage,
                    Background = page == _currentPage ? accentBackground : normalBackground,
                    Foreground = page == _currentPage ? activeForeground : normalForeground,
                });

                previousPage = page;
            }

            PageButtons.Add(new PageButtonItem
            {
                Label = "Next →",
                PageNumber = _currentPage + 1,
                IsEnabled = _currentPage < TotalPages,
                IsCurrent = false,
                Background = normalBackground,
                Foreground = normalForeground,
            });
        }

        private void SeedProducts()
        {
            var seeded = new[]
            {
                new ProductRowViewModel(1, "SUN-001", "Casual Sunglass", "Accessories", 124, 47m, true),
                new ProductRowViewModel(2, "TEE-001", "T-Shirt", "Clothes", 124, 47m, true),
                new ProductRowViewModel(3, "TEA-001", "Green Tea", "Beauty", 0, 47m, false),
                new ProductRowViewModel(4, "DEN-001", "Denim Shirt", "Clothes", 124, 47m, false),
                new ProductRowViewModel(5, "JCK-001", "Casual Jacket", "Clothes", 0, 47m, false),
                new ProductRowViewModel(6, "CAP-001", "Cap", "Accessories", 124, 47m, true),
                new ProductRowViewModel(7, "SHO-001", "Nike Cats", "Shoes", 124, 47m, false),
                new ProductRowViewModel(8, "FAN-001", "Cooling Fan", "Electronics", 124, 47m, false),
                new ProductRowViewModel(9, "WAT-001", "Man Watch", "Accessories", 124, 47m, false),
                new ProductRowViewModel(10, "LAP-001", "Laptop Stand", "Accessories", 46, 52m, true),
                new ProductRowViewModel(11, "MOU-001", "Gaming Mouse", "Electronics", 6, 31m, true),
                new ProductRowViewModel(12, "KEY-001", "Mechanical Keyboard", "Electronics", 2, 69m, true),
                new ProductRowViewModel(13, "BAG-001", "Crossbody Bag", "Accessories", 0, 39m, false),
                new ProductRowViewModel(14, "PWR-001", "Power Bank", "Electronics", 76, 29m, true),
            };

            foreach (var row in seeded)
            {
                row.PropertyChanged += Row_PropertyChanged;
                _allProducts.Add(row);
            }
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ProductRowViewModel.IsSelected))
            {
                return;
            }

            NotifySelectionChanged();
            UpdateSelectAllState();
        }

        private void UpdateSelectAllState()
        {
            if (PagedProducts.Count == 0)
            {
                IsAllOnPageSelected = false;
                return;
            }

            var allSelected = PagedProducts.All(p => p.IsSelected);
            if (IsAllOnPageSelected != allSelected)
            {
                _isAllOnPageSelected = allSelected;
                OnPropertyChanged(nameof(IsAllOnPageSelected));
            }
        }

        private void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectionActionText));
        }

        private static IEnumerable<int> BuildPageNumbers(int currentPage, int totalPages)
        {
            var pages = new SortedSet<int> { 1, totalPages };
            for (var offset = -1; offset <= 1; offset++)
            {
                var value = currentPage + offset;
                if (value >= 1 && value <= totalPages)
                {
                    pages.Add(value);
                }
            }

            return pages;
        }
    }
}
