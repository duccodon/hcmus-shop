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

namespace hcmus_shop.ViewModels.Products
{
    public class AddProductViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly ISeriesService _seriesService;
        private readonly IFileUploadService _fileUploadService;
        private ImagePreview _mainPreview = new();
        private int? _selectedBrandId;
        private int? _selectedSeriesId;
        private bool _isSaving;
        private bool _isAddingCategory;
        private bool _isInitialized;
        private string _saveErrorMessage = string.Empty;
        private string _categoryErrorMessage = string.Empty;
        private string _newCategoryName = string.Empty;
        private string _newCategoryDescription = string.Empty;
        private string _saveStatusMessage = string.Empty;

        public AddProductViewModel(
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

            SelectPreviewCommand = new RelayCommand<ImagePreview?>(SelectPreview);
            AddCategoryCommand = new AsyncRelayCommand(AddCategoryAsync, () => !IsAddingCategory);
            SaveProductCommand = new AsyncRelayCommand(SaveProductAsync, () => !IsSaving);
            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized);

            DraftProduct = new CreateProductInput
            {
                Sku = string.Empty,
                Name = string.Empty,
                Description = string.Empty,
                WarrantyMonths = 12,
                ImportPrice = 0,
                SellingPrice = 0,
                BrandId = 0,
            };
        }

        public CreateProductInput DraftProduct { get; }

        public ObservableCollection<ImagePreview> PreviewImages { get; } = [];

        public ObservableCollection<LookupOptionViewModel> BrandOptions { get; } = [];

        public ObservableCollection<LookupOptionViewModel> SeriesOptions { get; } = [];

        public ObservableCollection<CategoryOptionViewModel> CategoryOptions { get; } = [];

        public IRelayCommand<ImagePreview?> SelectPreviewCommand { get; }

        public IAsyncRelayCommand AddCategoryCommand { get; }

        public IAsyncRelayCommand SaveProductCommand { get; }

        public IAsyncRelayCommand InitializeCommand { get; }

        public event EventHandler? ProductSaved;
        public Func<Task<NewCategoryInput?>>? RequestCategoryInputAsync { get; set; }

        public bool IsInitialized
        {
            get => _isInitialized;
            private set
            {
                if (SetProperty(ref _isInitialized, value))
                {
                    InitializeCommand.NotifyCanExecuteChanged();
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
                    SaveProductCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsAddingCategory
        {
            get => _isAddingCategory;
            private set
            {
                if (SetProperty(ref _isAddingCategory, value))
                {
                    AddCategoryCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(AddCategoryButtonText));
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

        public string CategoryErrorMessage
        {
            get => _categoryErrorMessage;
            private set
            {
                if (SetProperty(ref _categoryErrorMessage, value))
                {
                    OnPropertyChanged(nameof(HasCategoryError));
                }
            }
        }

        public bool HasCategoryError => !string.IsNullOrWhiteSpace(CategoryErrorMessage);

        public string NewCategoryName
        {
            get => _newCategoryName;
            private set => SetProperty(ref _newCategoryName, value);
        }

        public string NewCategoryDescription
        {
            get => _newCategoryDescription;
            private set => SetProperty(ref _newCategoryDescription, value);
        }

        public string AddCategoryButtonText => IsAddingCategory ? "Adding..." : "Add Category";

        public int? SelectedBrandId
        {
            get => _selectedBrandId;
            set
            {
                if (SetProperty(ref _selectedBrandId, value))
                {
                    DraftProduct.BrandId = value ?? 0;
                    SelectedSeriesId = null;
                    _ = LoadSeriesOptionsAsync();
                }
            }
        }

        public int? SelectedSeriesId
        {
            get => _selectedSeriesId;
            set
            {
                if (SetProperty(ref _selectedSeriesId, value))
                {
                    DraftProduct.SeriesId = value;
                }
            }
        }

        public ImagePreview MainPreview
        {
            get => _mainPreview;
            private set
            {
                if (SetProperty(ref _mainPreview, value))
                {
                    OnPropertyChanged(nameof(HasMainPreview));
                }
            }
        }

        public bool HasMainPreview => MainPreview.Bitmap is not null;

        public double ImportPriceValue
        {
            get => DraftProduct.ImportPrice;
            set
            {
                DraftProduct.ImportPrice = value;
                OnPropertyChanged(nameof(ImportPriceValue));
            }
        }

        public double SellingPriceValue
        {
            get => DraftProduct.SellingPrice;
            set
            {
                DraftProduct.SellingPrice = value;
                OnPropertyChanged(nameof(SellingPriceValue));
            }
        }

        public double WarrantyMonthsValue
        {
            get => DraftProduct.WarrantyMonths;
            set
            {
                DraftProduct.WarrantyMonths = Math.Max(0, Convert.ToInt32(value));
                OnPropertyChanged(nameof(WarrantyMonthsValue));
            }
        }

        public void AddImagePreview(ImagePreview preview)
        {
            preview.DisplayOrder = PreviewImages.Count;
            PreviewImages.Add(preview);

            if (!HasMainPreview)
            {
                MainPreview = preview;
            }
        }

        private void SelectPreview(ImagePreview? preview)
        {
            if (preview is null)
            {
                return;
            }

            MainPreview = preview;
        }

        private async Task AddCategoryAsync()
        {
            if (IsAddingCategory)
            {
                return;
            }

            CategoryErrorMessage = string.Empty;

            if (RequestCategoryInputAsync is null)
            {
                CategoryErrorMessage = "Category dialog is not available.";
                return;
            }

            var input = await RequestCategoryInputAsync();
            if (input is null)
            {
                return;
            }

            NewCategoryName = input.Name?.Trim() ?? string.Empty;
            NewCategoryDescription = input.Description?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                CategoryErrorMessage = "Category name is required.";
                return;
            }

            if (CategoryOptions.Any(option =>
                string.Equals(option.Name, NewCategoryName, StringComparison.OrdinalIgnoreCase)))
            {
                CategoryErrorMessage = "Category already exists.";
                return;
            }

            IsAddingCategory = true;

            try
            {
                var created = await _categoryService.CreateAsync(
                    NewCategoryName,
                    string.IsNullOrWhiteSpace(NewCategoryDescription) ? null : NewCategoryDescription);

                await ReloadCategoryOptionsAsync(created.CategoryId);

                NewCategoryName = string.Empty;
                NewCategoryDescription = string.Empty;
                CategoryErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                CategoryErrorMessage = ex.Message;
            }
            finally
            {
                IsAddingCategory = false;
            }
        }

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            var brands = await _brandService.GetAllAsync();

            BrandOptions.Clear();
            foreach (var brand in brands)
            {
                BrandOptions.Add(new LookupOptionViewModel(brand.BrandId, brand.Name));
            }

            await ReloadCategoryOptionsAsync();

            SelectedBrandId = BrandOptions.FirstOrDefault()?.Id;
            IsInitialized = true;
        }

        private async Task ReloadCategoryOptionsAsync(int? categoryToSelect = null)
        {
            var selectedIds = new HashSet<int>(
                CategoryOptions.Where(option => option.IsSelected).Select(option => option.CategoryId));

            if (categoryToSelect.HasValue)
            {
                selectedIds.Add(categoryToSelect.Value);
            }

            var categories = await _categoryService.GetAllAsync();

            CategoryOptions.Clear();
            foreach (var category in categories.OrderBy(category => category.Name))
            {
                CategoryOptions.Add(new CategoryOptionViewModel(category.CategoryId, category.Name)
                {
                    IsSelected = selectedIds.Contains(category.CategoryId)
                });
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
            foreach (var series in seriesItems)
            {
                SeriesOptions.Add(new LookupOptionViewModel(series.SeriesId, series.Name));
            }
        }

        private async Task SaveProductAsync()
        {
            if (IsSaving)
            {
                return;
            }

            SaveErrorMessage = string.Empty;
            SaveStatusMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(DraftProduct.Sku) || string.IsNullOrWhiteSpace(DraftProduct.Name))
            {
                SaveErrorMessage = "SKU and product name are required.";
                return;
            }

            if (DraftProduct.BrandId <= 0)
            {
                SaveErrorMessage = "Please select a brand.";
                return;
            }

            DraftProduct.CategoryIds =
            [
                .. CategoryOptions
                    .Where(option => option.IsSelected)
                    .Select(option => option.CategoryId)
            ];

            IsSaving = true;
            try
            {
                SaveStatusMessage = "Uploading images...";
                var uploadedImageUrls = new List<string>();
                foreach (var preview in PreviewImages.Where(preview => preview.File is not null))
                {
                    var imageUrl = await _fileUploadService.UploadImageAsync(preview.File!);
                    uploadedImageUrls.Add(imageUrl);
                }

                DraftProduct.ImageUrls = uploadedImageUrls;
                SaveStatusMessage = "Saving product...";
                await _productService.CreateAsync(DraftProduct);
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

    }

    public class NewCategoryInput
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
