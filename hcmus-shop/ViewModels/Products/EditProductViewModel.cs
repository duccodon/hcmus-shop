using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Brands;
using hcmus_shop.Services.Categories;
using hcmus_shop.Services.Products;
using hcmus_shop.Services.Series;
using hcmus_shop.Services.Uploads;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace hcmus_shop.ViewModels.Products
{
    public class EditProductViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly ISeriesService _seriesService;
        private readonly IFileUploadService _fileUploadService;

        private int _productId;
        private bool _isInitialized;
        private bool _isLoading;
        private bool _isSaving;
        private bool _isDeleting;
        private bool _suppressSeriesReload;
        private string _saveErrorMessage = string.Empty;
        private string _sku = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private int? _selectedBrandId;
        private int? _selectedSeriesId;
        private double _importPrice;
        private double _sellingPrice;
        private double _warrantyMonths = 12;
        private bool _isActive = true;
        private string _newImageUrl = string.Empty;
        private string _saveStatusMessage = string.Empty;

        public EditProductViewModel(
            IProductService productService,
            IBrandService brandService,
            ICategoryService categoryService,
            ISeriesService seriesService,
            IFileUploadService fileUploadService)
        {
            _productService = productService;
            _brandService = brandService;
            _categoryService = categoryService;
            _seriesService = seriesService;
            _fileUploadService = fileUploadService;

            SaveProductCommand = new AsyncRelayCommand(SaveProductAsync, CanSaveOrDelete);
            DeleteProductCommand = new AsyncRelayCommand(DeleteProductAsync, CanSaveOrDelete);
            AddImageUrlCommand = new RelayCommand(AddImageUrl);
            RemoveImageUrlCommand = new RelayCommand<string>(RemoveImageUrl);
        }

        public ObservableCollection<LookupOptionViewModel> BrandOptions { get; } = [];
        public ObservableCollection<LookupOptionViewModel> SeriesOptions { get; } = [];
        public ObservableCollection<CategoryOptionViewModel> CategoryOptions { get; } = [];
        public ObservableCollection<string> ImageUrls { get; } = [];
        public ObservableCollection<PendingImageFileViewModel> PendingImageFiles { get; } = [];

        public IAsyncRelayCommand SaveProductCommand { get; }
        public IAsyncRelayCommand DeleteProductCommand { get; }
        public IRelayCommand AddImageUrlCommand { get; }
        public IRelayCommand<string> RemoveImageUrlCommand { get; }

        public event EventHandler? ProductSaved;
        public event EventHandler? ProductDeleted;
        public Func<Task<bool>>? DeleteConfirmationRequested;

        public bool IsInitialized
        {
            get => _isInitialized;
            private set => SetProperty(ref _isInitialized, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    NotifyStateChanged();
                }
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            private set
            {
                if (SetProperty(ref _isSaving, value))
                {
                    NotifyStateChanged();
                }
            }
        }

        public bool IsDeleting
        {
            get => _isDeleting;
            private set
            {
                if (SetProperty(ref _isDeleting, value))
                {
                    NotifyStateChanged();
                }
            }
        }

        public string SaveErrorMessage
        {
            get => _saveErrorMessage;
            private set => SetProperty(ref _saveErrorMessage, value);
        }

        public string SaveStatusMessage
        {
            get => _saveStatusMessage;
            private set
            {
                if (SetProperty(ref _saveStatusMessage, value))
                {
                    OnPropertyChanged(nameof(HasSaveStatus));
                }
            }
        }

        public bool HasSaveStatus => !string.IsNullOrWhiteSpace(SaveStatusMessage);

        public string Sku
        {
            get => _sku;
            set => SetProperty(ref _sku, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public int? SelectedBrandId
        {
            get => _selectedBrandId;
            set
            {
                if (SetProperty(ref _selectedBrandId, value))
                {
                    if (_suppressSeriesReload)
                    {
                        return;
                    }

                    SelectedSeriesId = null;
                    _ = LoadSeriesOptionsAsync();
                }
            }
        }

        public int? SelectedSeriesId
        {
            get => _selectedSeriesId;
            set => SetProperty(ref _selectedSeriesId, value);
        }

        public double ImportPriceValue
        {
            get => _importPrice;
            set => SetProperty(ref _importPrice, value);
        }

        public double SellingPriceValue
        {
            get => _sellingPrice;
            set => SetProperty(ref _sellingPrice, value);
        }

        public double WarrantyMonthsValue
        {
            get => _warrantyMonths;
            set => SetProperty(ref _warrantyMonths, Math.Max(0, value));
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string NewImageUrl
        {
            get => _newImageUrl;
            set => SetProperty(ref _newImageUrl, value);
        }

        public async Task InitializeAsync(int productId)
        {
            if (productId <= 0)
            {
                SaveErrorMessage = "Invalid product id.";
                return;
            }

            IsLoading = true;
            SaveErrorMessage = string.Empty;
            SaveStatusMessage = string.Empty;

            try
            {
                _productId = productId;
                await LoadLookupOptionsAsync();
                await LoadProductAsync(productId);
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                SaveErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadLookupOptionsAsync()
        {
            var brandsTask = _brandService.GetAllAsync();
            var categoriesTask = _categoryService.GetAllAsync();

            await Task.WhenAll(brandsTask, categoriesTask);

            BrandOptions.Clear();
            foreach (var brand in brandsTask.Result.OrderBy(b => b.Name))
            {
                BrandOptions.Add(new LookupOptionViewModel(brand.BrandId, brand.Name));
            }

            CategoryOptions.Clear();
            foreach (var category in categoriesTask.Result.OrderBy(c => c.Name))
            {
                CategoryOptions.Add(new CategoryOptionViewModel(category.CategoryId, category.Name));
            }
        }

        private async Task LoadProductAsync(int productId)
        {
            var product = await _productService.GetByIdAsync(productId);
            if (product is null)
            {
                throw new InvalidOperationException("Product not found.");
            }

            Sku = product.Sku;
            Name = product.Name;
            Description = product.Description ?? string.Empty;
            ImportPriceValue = product.ImportPrice;
            SellingPriceValue = product.SellingPrice;
            WarrantyMonthsValue = product.WarrantyMonths;
            IsActive = product.IsActive;

            _suppressSeriesReload = true;
            SelectedBrandId = product.Brand?.BrandId > 0 ? product.Brand.BrandId : (int?)null;
            _suppressSeriesReload = false;

            await LoadSeriesOptionsAsync();
            SelectedSeriesId = product.Series?.SeriesId;

            var selectedCategories = new HashSet<int>(product.Categories.Select(c => c.CategoryId));
            foreach (var category in CategoryOptions)
            {
                category.IsSelected = selectedCategories.Contains(category.CategoryId);
            }

            ImageUrls.Clear();
            foreach (var image in product.Images.OrderBy(i => i.DisplayOrder))
            {
                if (!string.IsNullOrWhiteSpace(image.ImageUrl))
                {
                    ImageUrls.Add(image.ImageUrl);
                }
            }
        }

        private async Task LoadSeriesOptionsAsync()
        {
            SeriesOptions.Clear();
            if (!SelectedBrandId.HasValue)
            {
                return;
            }

            var seriesItems = await _seriesService.GetByBrandAsync(SelectedBrandId.Value);
            foreach (var series in seriesItems.OrderBy(s => s.Name))
            {
                SeriesOptions.Add(new LookupOptionViewModel(series.SeriesId, series.Name));
            }
        }

        private async Task SaveProductAsync()
        {
            if (!CanSaveOrDelete())
            {
                return;
            }

            SaveErrorMessage = string.Empty;
            SaveStatusMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Sku) || string.IsNullOrWhiteSpace(Name))
            {
                SaveErrorMessage = "SKU and product name are required.";
                return;
            }

            if (!SelectedBrandId.HasValue || SelectedBrandId.Value <= 0)
            {
                SaveErrorMessage = "Please select a brand.";
                return;
            }

            IsSaving = true;
            try
            {
                SaveStatusMessage = "Uploading images...";
                var mergedImageUrls = ImageUrls
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select(url => url.Trim())
                    .ToList();

                foreach (var pendingFile in PendingImageFiles.ToList())
                {
                    var uploadedUrl = await _fileUploadService.UploadImageAsync(pendingFile.File);
                    mergedImageUrls.Add(uploadedUrl);
                }

                SaveStatusMessage = "Saving product...";
                await _productService.UpdateAsync(_productId, new UpdateProductInput
                {
                    Sku = Sku.Trim(),
                    Name = Name.Trim(),
                    BrandId = SelectedBrandId,
                    SeriesId = SelectedSeriesId,
                    ImportPrice = ImportPriceValue,
                    SellingPrice = SellingPriceValue,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    WarrantyMonths = Math.Max(0, (int)Math.Round(WarrantyMonthsValue)),
                    IsActive = IsActive,
                    CategoryIds =
                    [
                        .. CategoryOptions
                            .Where(option => option.IsSelected)
                            .Select(option => option.CategoryId)
                    ],
                    ImageUrls =
                    [
                        .. mergedImageUrls
                    ]
                });

                PendingImageFiles.Clear();
                SaveStatusMessage = string.Empty;
                ProductSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                SaveStatusMessage = string.Empty;
                SaveErrorMessage = ex.Message;
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task DeleteProductAsync()
        {
            if (!CanSaveOrDelete())
            {
                return;
            }

            SaveErrorMessage = string.Empty;

            var confirmed = DeleteConfirmationRequested is null || await DeleteConfirmationRequested();
            if (!confirmed)
            {
                return;
            }

            IsDeleting = true;
            try
            {
                await _productService.DeleteAsync(_productId);
                ProductDeleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                SaveErrorMessage = ex.Message;
            }
            finally
            {
                IsDeleting = false;
            }
        }

        private void AddImageUrl()
        {
            var value = NewImageUrl?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (ImageUrls.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                NewImageUrl = string.Empty;
                return;
            }

            ImageUrls.Add(value);
            NewImageUrl = string.Empty;
        }

        public void AddPendingImageFile(StorageFile file)
        {
            if (file is null)
            {
                return;
            }

            if (PendingImageFiles.Any(item =>
                    string.Equals(item.File.Path, file.Path, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            PendingImageFiles.Add(new PendingImageFileViewModel(file));
        }

        public void RemovePendingImageFile(PendingImageFileViewModel? item)
        {
            if (item is null)
            {
                return;
            }

            PendingImageFiles.Remove(item);
        }

        private void RemoveImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            ImageUrls.Remove(imageUrl);
        }

        private bool CanSaveOrDelete()
        {
            return !IsLoading && !IsSaving && !IsDeleting;
        }

        private void NotifyStateChanged()
        {
            SaveProductCommand.NotifyCanExecuteChanged();
            DeleteProductCommand.NotifyCanExecuteChanged();
        }
    }

    public class PendingImageFileViewModel
    {
        public PendingImageFileViewModel(StorageFile file)
        {
            File = file;
            Name = file.Name;
        }

        public StorageFile File { get; }
        public string Name { get; }
    }
}
