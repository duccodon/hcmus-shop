using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Products
{
    public partial class ProductsViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private CancellationTokenSource? _searchDebounceCts;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private int _selectedPageSize = 10;

        [ObservableProperty]
        private bool _isAllOnPageSelected;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isInitialized;

        [ObservableProperty]
        private int? _selectedCategoryId;

        [ObservableProperty]
        private int? _selectedBrandId;

        private int _currentPage = 1;
        private int _totalCount;
        private readonly string _sortBy = "createdAt";
        private readonly string _sortOrder = "desc";
        private int _loadVersion;
        private bool _suppressFilterReload;
        private bool _suppressSelectAllChange;

        public ProductsViewModel(
            IProductService productService,
            IBrandService brandService,
            ICategoryService categoryService)
        {
            _productService = productService;
            _brandService = brandService;
            _categoryService = categoryService;
        }

        public ObservableCollection<ProductRowViewModel> PagedProducts { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [10, 20, 50];
        public ObservableCollection<PageButtonItem> PageButtons { get; } = [];
        public ObservableCollection<FilterOptionViewModel> CategoryOptions { get; } = [];
        public ObservableCollection<FilterOptionViewModel> BrandOptions { get; } = [];

        public event EventHandler? NavigateToAddProductRequested;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool IsEmpty => !IsBusy && !HasError && PagedProducts.Count == 0;
        public bool HasSelection => PagedProducts.Any(p => p.IsSelected);
        public string SelectionActionText => $"{PagedProducts.Count(p => p.IsSelected)} selected";

        public string ResultText
        {
            get
            {
                if (_totalCount == 0)
                {
                    return "Result 0 of 0";
                }
                var start = ((_currentPage - 1) * SelectedPageSize) + 1;
                var end = Math.Min(_currentPage * SelectedPageSize, _totalCount);
                return $"Result {start}-{end} of {_totalCount}";
            }
        }

        private int TotalPages =>
            Math.Max(1, (int)Math.Ceiling((double)Math.Max(_totalCount, 1) / SelectedPageSize));

        [RelayCommand]
        private void AddProduct()
        {
            NavigateToAddProductRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand(CanExecute = nameof(CanInitialize))]
        private async Task InitializeAsync()
        {
            if (IsInitialized) return;

            try
            {
                IsBusy = true;
                ErrorMessage = null;
                await LoadFilterOptionsAsync();
                await LoadProductsAsync(clearError: false);
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to initialize products page: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanModifyPage))]
        private async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPages || page == _currentPage) return;
            _currentPage = page;
            await LoadProductsAsync();
        }

        [RelayCommand(CanExecute = nameof(CanModifyPage))]
        private async Task ClearFiltersAsync()
        {
            _suppressFilterReload = true;
            SelectedCategoryId = null;
            SelectedBrandId = null;
            _suppressFilterReload = false;
            _currentPage = 1;
            await LoadProductsAsync();
        }

        private async Task LoadFilterOptionsAsync()
        {
            try
            {
                var brandsTask = _brandService.GetAllAsync();
                var categoriesTask = _categoryService.GetAllAsync();
                await Task.WhenAll(brandsTask, categoriesTask);

                BrandOptions.Clear();
                BrandOptions.Add(new FilterOptionViewModel(null, "All brands"));
                if (brandsTask.Result.IsSuccess && brandsTask.Result.Value != null)
                {
                    foreach (var brand in brandsTask.Result.Value.OrderBy(b => b.Name))
                    {
                        BrandOptions.Add(new FilterOptionViewModel(brand.BrandId, brand.Name));
                    }
                }

                CategoryOptions.Clear();
                CategoryOptions.Add(new FilterOptionViewModel(null, "All categories"));
                if (categoriesTask.Result.IsSuccess && categoriesTask.Result.Value != null)
                {
                    foreach (var category in categoriesTask.Result.Value.OrderBy(c => c.Name))
                    {
                        CategoryOptions.Add(new FilterOptionViewModel(category.CategoryId, category.Name));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load filters: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanModifyPage))]
        private async Task BulkToggleStatusAsync()
        {
            var selectedRows = PagedProducts.Where(p => p.IsSelected).ToList();
            if (selectedRows.Count == 0) return;

            IsBusy = true;
            ErrorMessage = null;
            try
            {
                foreach (var row in selectedRows)
                {
                    await _productService.UpdateAsync(row.ProductId, new UpdateProductInput
                    {
                        IsActive = !row.IsActive
                    });
                }
                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanModifyPage))]
        private async Task BulkDeleteAsync()
        {
            var selectedIds = PagedProducts
                .Where(p => p.IsSelected)
                .Select(p => p.ProductId)
                .ToList();
            if (selectedIds.Count == 0) return;

            IsBusy = true;
            ErrorMessage = null;
            try
            {
                foreach (var productId in selectedIds)
                {
                    await _productService.DeleteAsync(productId);
                }

                if ((_currentPage - 1) * SelectedPageSize >= Math.Max(_totalCount - selectedIds.Count, 0) && _currentPage > 1)
                {
                    _currentPage--;
                }
                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadProductsAsync(bool clearError = true)
        {
            var requestVersion = ++_loadVersion;
            IsBusy = true;
            if (clearError) ErrorMessage = null;

            try
            {
                var result = await _productService.GetAllAsync(new ProductFilterDto
                {
                    Search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                    CategoryId = SelectedCategoryId,
                    BrandId = SelectedBrandId,
                    SortBy = _sortBy,
                    SortOrder = _sortOrder,
                    Page = _currentPage,
                    PageSize = SelectedPageSize
                });

                if (requestVersion != _loadVersion) return;

                if (!result.IsSuccess)
                {
                    if (requestVersion == _loadVersion)
                    {
                        _totalCount = 0;
                        PagedProducts.Clear();
                        RebuildPageButtons();
                        UpdateSelectAllState();
                        NotifySelectionChanged();
                        OnPropertyChanged(nameof(ResultText));
                        OnPropertyChanged(nameof(IsEmpty));
                        ErrorMessage = result.Error ?? "Failed to load products";
                    }
                    return;
                }

                var page = result.Value!;
                _totalCount = page.TotalCount;

                if (_currentPage > TotalPages) _currentPage = TotalPages;

                PagedProducts.Clear();
                foreach (var item in page.Items)
                {
                    var row = new ProductRowViewModel(
                        item.ProductId,
                        item.Sku,
                        item.Name,
                        item.Categories.Count > 0 ? string.Join(", ", item.Categories.Select(c => c.Name)) : "Uncategorized",
                        item.StockQuantity,
                        Convert.ToDecimal(item.SellingPrice),
                        item.IsActive);
                    row.PropertyChanged += Row_PropertyChanged;
                    PagedProducts.Add(row);
                }

                RebuildPageButtons();
                UpdateSelectAllState();
                NotifySelectionChanged();
                OnPropertyChanged(nameof(ResultText));
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception ex)
            {
                if (requestVersion == _loadVersion)
                {
                    _totalCount = 0;
                    PagedProducts.Clear();
                    RebuildPageButtons();
                    UpdateSelectAllState();
                    NotifySelectionChanged();
                    OnPropertyChanged(nameof(ResultText));
                    OnPropertyChanged(nameof(IsEmpty));
                    ErrorMessage = ex.Message;
                }
            }
            finally
            {
                if (requestVersion == _loadVersion) IsBusy = false;
            }
        }

        private void DebounceSearch()
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts?.Dispose();
            _searchDebounceCts = new CancellationTokenSource();
            var token = _searchDebounceCts.Token;
            _ = DebounceSearchAsync(token);
        }

        private async Task DebounceSearchAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(400, token);
                if (!token.IsCancellationRequested) await LoadProductsAsync();
            }
            catch (TaskCanceledException) { }
        }

        private bool CanInitialize() => !IsBusy && !IsInitialized;
        private bool CanModifyPage() => !IsBusy;

        private void UpdateSelectAll(bool value)
        {
            _suppressSelectAllChange = true;
            IsAllOnPageSelected = value;
            _suppressSelectAllChange = false;
        }

        partial void OnSearchQueryChanged(string value)
        {
            _currentPage = 1;
            DebounceSearch();
        }

        partial void OnSelectedPageSizeChanged(int value)
        {
            _currentPage = 1;
            _ = LoadProductsAsync();
        }

        partial void OnIsAllOnPageSelectedChanged(bool value)
        {
            if (_suppressSelectAllChange) return;
            foreach (var row in PagedProducts) row.IsSelected = value;
            NotifySelectionChanged();
        }

        partial void OnIsBusyChanged(bool value)
        {
            InitializeCommand.NotifyCanExecuteChanged();
            ClearFiltersCommand.NotifyCanExecuteChanged();
            GoToPageCommand.NotifyCanExecuteChanged();
            BulkToggleStatusCommand.NotifyCanExecuteChanged();
            BulkDeleteCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }

        partial void OnErrorMessageChanged(string? value)
        {
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(IsEmpty));
        }

        partial void OnSelectedCategoryIdChanged(int? value)
        {
            if (_suppressFilterReload) return;
            _currentPage = 1;
            _ = LoadProductsAsync();
        }

        partial void OnSelectedBrandIdChanged(int? value)
        {
            if (_suppressFilterReload) return;
            _currentPage = 1;
            _ = LoadProductsAsync();
        }

        partial void OnIsInitializedChanged(bool value)
        {
            InitializeCommand.NotifyCanExecuteChanged();
        }

        private void RebuildPageButtons()
        {
            PageButtons.Clear();
            PageButtons.Add(new PageButtonItem
            {
                Label = "<- Previous",
                PageNumber = _currentPage - 1,
                IsEnabled = _currentPage > 1,
                IsCurrent = false,
            });
            var pages = BuildPageNumbers(_currentPage, TotalPages);
            int? previousPage = null;
            foreach (var page in pages)
            {
                if (previousPage.HasValue && page - previousPage.Value > 1)
                {
                    PageButtons.Add(new PageButtonItem
                    {
                        Label = "...",
                        PageNumber = -1,
                        IsEnabled = false,
                        IsCurrent = false,
                    });
                }
                PageButtons.Add(new PageButtonItem
                {
                    Label = page.ToString(),
                    PageNumber = page,
                    IsEnabled = page != _currentPage,
                    IsCurrent = page == _currentPage,
                });
                previousPage = page;
            }
            PageButtons.Add(new PageButtonItem
            {
                Label = "Next ->",
                PageNumber = _currentPage + 1,
                IsEnabled = _currentPage < TotalPages,
                IsCurrent = false,
            });
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ProductRowViewModel.IsSelected)) return;
            NotifySelectionChanged();
            UpdateSelectAllState();
        }

        private void UpdateSelectAllState()
        {
            if (PagedProducts.Count == 0)
            {
                UpdateSelectAll(false);
                return;
            }
            var allSelected = PagedProducts.All(p => p.IsSelected);
            if (IsAllOnPageSelected != allSelected)
            {
                UpdateSelectAll(allSelected);
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