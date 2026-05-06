using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Services.Products.Dto;
using hcmus_shop.Services.Uploads;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace hcmus_shop.ViewModels.Products
{
    public class AddProductViewModel : ObservableObject
    {
        private const int MinimumImageCount = 1;
        private const string DraftStorageKey = "AddProductDraft";

        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly ISeriesService _seriesService;
        private readonly IFileUploadService _fileUploadService;
        private readonly DispatcherTimer _autoSaveTimer;

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
        private bool _isDraftDirty;
        private bool _isRestoringDraft;
        private bool _hasSavedDraft;
        private bool _isDraftRestored;
        private bool _isAutoSaveActive;
        private bool _suppressSeriesReload;

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
            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;

            SelectPreviewCommand = new RelayCommand<ImagePreview?>(SelectPreview);
            AddCategoryCommand = new AsyncRelayCommand(AddCategoryAsync, () => !IsAddingCategory);
            SaveProductCommand = new AsyncRelayCommand(SaveProductAsync, () => !IsSaving);
            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized);
            DiscardDraftCommand = new AsyncRelayCommand(DiscardDraftAsync);

            DraftProduct = new CreateProductInput
            {
                Sku = string.Empty,
                Name = string.Empty,
                Description = string.Empty,
                WarrantyMonths = 12,
                ImportPrice = 0,
                SellingPrice = 0,
                StockQuantity = 0,
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
        public IAsyncRelayCommand DiscardDraftCommand { get; }

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
                    OnPropertyChanged(nameof(IsBusy));
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
                    OnPropertyChanged(nameof(IsBusy));
                }
            }
        }

        public bool IsBusy => IsSaving || IsAddingCategory;

        public string SaveErrorMessage
        {
            get => _saveErrorMessage;
            private set
            {
                if (SetProperty(ref _saveErrorMessage, value))
                {
                    OnPropertyChanged(nameof(HasSaveError));
                }
            }
        }

        public bool HasSaveError => !string.IsNullOrWhiteSpace(SaveErrorMessage);

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

        public bool HasSavedDraft
        {
            get => _hasSavedDraft;
            private set
            {
                if (SetProperty(ref _hasSavedDraft, value))
                {
                    OnPropertyChanged(nameof(DraftStatusText));
                }
            }
        }

        public bool IsDraftRestored
        {
            get => _isDraftRestored;
            private set
            {
                if (SetProperty(ref _isDraftRestored, value))
                {
                    OnPropertyChanged(nameof(DraftStatusText));
                }
            }
        }

        public string DraftStatusText =>
            IsDraftRestored ? "Draft restored automatically." :
            HasSavedDraft ? "Draft auto-save is active." :
            string.Empty;

        public int? SelectedBrandId
        {
            get => _selectedBrandId;
            set
            {
                if (SetProperty(ref _selectedBrandId, value))
                {
                    DraftProduct.BrandId = value ?? 0;
                    MarkDraftDirty();

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
            set
            {
                if (SetProperty(ref _selectedSeriesId, value))
                {
                    DraftProduct.SeriesId = value;
                    MarkDraftDirty();
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
                MarkDraftDirty();
            }
        }

        public double SellingPriceValue
        {
            get => DraftProduct.SellingPrice;
            set
            {
                DraftProduct.SellingPrice = value;
                OnPropertyChanged(nameof(SellingPriceValue));
                MarkDraftDirty();
            }
        }

        public double WarrantyMonthsValue
        {
            get => DraftProduct.WarrantyMonths;
            set
            {
                DraftProduct.WarrantyMonths = Math.Max(0, Convert.ToInt32(value));
                OnPropertyChanged(nameof(WarrantyMonthsValue));
                MarkDraftDirty();
            }
        }

        public double StockQuantityValue
        {
            get => DraftProduct.StockQuantity;
            set
            {
                DraftProduct.StockQuantity = Math.Max(0, Convert.ToInt32(value));
                OnPropertyChanged(nameof(StockQuantityValue));
                MarkDraftDirty();
            }
        }

        public string Sku
        {
            get => DraftProduct.Sku;
            set
            {
                if (DraftProduct.Sku == value)
                {
                    return;
                }

                DraftProduct.Sku = value;
                OnPropertyChanged(nameof(Sku));
                MarkDraftDirty();
            }
        }

        public string ProductName
        {
            get => DraftProduct.Name;
            set
            {
                if (DraftProduct.Name == value)
                {
                    return;
                }

                DraftProduct.Name = value;
                OnPropertyChanged(nameof(ProductName));
                MarkDraftDirty();
            }
        }

        public string ProductDescription
        {
            get => DraftProduct.Description ?? string.Empty;
            set
            {
                if ((DraftProduct.Description ?? string.Empty) == value)
                {
                    return;
                }

                DraftProduct.Description = value;
                OnPropertyChanged(nameof(ProductDescription));
                MarkDraftDirty();
            }
        }

        public string Specifications
        {
            get => DraftProduct.Specifications ?? string.Empty;
            set
            {
                if ((DraftProduct.Specifications ?? string.Empty) == value)
                {
                    return;
                }

                DraftProduct.Specifications = value;
                OnPropertyChanged(nameof(Specifications));
                MarkDraftDirty();
            }
        }

        public void StartAutoSave()
        {
            if (_isAutoSaveActive)
            {
                return;
            }

            _isAutoSaveActive = true;
            _autoSaveTimer.Start();
        }

        public void StopAutoSave()
        {
            if (!_isAutoSaveActive)
            {
                return;
            }

            _isAutoSaveActive = false;
            _autoSaveTimer.Stop();
        }

        public void AddImagePreview(ImagePreview preview)
        {
            preview.DisplayOrder = PreviewImages.Count;
            PreviewImages.Add(preview);

            if (!HasMainPreview)
            {
                MainPreview = preview;
            }

            MarkDraftDirty();
        }

        private void SelectPreview(ImagePreview? preview)
        {
            if (preview is null)
            {
                return;
            }

            MainPreview = preview;
        }

        private async void AutoSaveTimer_Tick(object? sender, object e)
        {
            await SaveDraftIfDirtyAsync();
        }

        private void MarkDraftDirty()
        {
            if (_isRestoringDraft)
            {
                return;
            }

            _isDraftDirty = true;
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
                var createResult = await _categoryService.CreateAsync(
                    NewCategoryName,
                    string.IsNullOrWhiteSpace(NewCategoryDescription) ? null : NewCategoryDescription);

                if (!createResult.IsSuccess || createResult.Value is null)
                {
                    CategoryErrorMessage = createResult.Error ?? "Failed to create category.";
                    return;
                }

                await ReloadCategoryOptionsAsync(createResult.Value.CategoryId);

                NewCategoryName = string.Empty;
                NewCategoryDescription = string.Empty;
                CategoryErrorMessage = string.Empty;
                MarkDraftDirty();
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

            SaveErrorMessage = string.Empty;

            var brandsResult = await _brandService.GetAllAsync();
            if (!brandsResult.IsSuccess || brandsResult.Value is null)
            {
                SaveErrorMessage = brandsResult.Error ?? "Failed to load brands.";
                return;
            }

            BrandOptions.Clear();
            foreach (var brand in brandsResult.Value.OrderBy(brand => brand.Name))
            {
                BrandOptions.Add(new LookupOptionViewModel(brand.BrandId, brand.Name));
            }

            var categoriesLoaded = await ReloadCategoryOptionsAsync();
            if (!categoriesLoaded)
            {
                return;
            }

            var restored = await TryRestoreDraftAsync();
            if (!restored)
            {
                _isRestoringDraft = true;
                try
                {
                    SelectedBrandId = BrandOptions.FirstOrDefault()?.Id;
                }
                finally
                {
                    _isRestoringDraft = false;
                }

                HasSavedDraft = false;
                IsDraftRestored = false;
            }

            IsInitialized = true;
        }

        private async Task<bool> ReloadCategoryOptionsAsync(int? categoryToSelect = null)
        {
            var selectedIds = new HashSet<int>(
                CategoryOptions.Where(option => option.IsSelected).Select(option => option.CategoryId));

            if (categoryToSelect.HasValue)
            {
                selectedIds.Add(categoryToSelect.Value);
            }

            var categoriesResult = await _categoryService.GetAllAsync();
            if (!categoriesResult.IsSuccess || categoriesResult.Value is null)
            {
                CategoryErrorMessage = categoriesResult.Error ?? "Failed to load categories.";
                return false;
            }

            CategoryOptions.Clear();
            foreach (var category in categoriesResult.Value.OrderBy(category => category.Name))
            {
                var option = new CategoryOptionViewModel(category.CategoryId, category.Name)
                {
                    IsSelected = selectedIds.Contains(category.CategoryId)
                };
                option.PropertyChanged += CategoryOption_PropertyChanged;
                CategoryOptions.Add(option);
            }

            return true;
        }

        private void CategoryOption_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CategoryOptionViewModel.IsSelected))
            {
                MarkDraftDirty();
            }
        }

        private async Task LoadSeriesOptionsAsync()
        {
            SeriesOptions.Clear();
            if (!SelectedBrandId.HasValue)
            {
                return;
            }

            var seriesResult = await _seriesService.GetByBrandAsync(SelectedBrandId.Value);
            if (!seriesResult.IsSuccess || seriesResult.Value is null)
            {
                SaveErrorMessage = seriesResult.Error ?? "Failed to load series.";
                return;
            }

            foreach (var series in seriesResult.Value.OrderBy(series => series.Name))
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

            if (PreviewImages.Count < MinimumImageCount)
            {
                SaveErrorMessage = $"Please add at least {MinimumImageCount} product image.";
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

                var createResult = await _productService.CreateAsync(DraftProduct);
                if (!createResult.IsSuccess)
                {
                    SaveErrorMessage = createResult.Error ?? "Failed to save product.";
                    return;
                }

                await ClearDraftAsync(resetRestoredState: true);
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

        private async Task<bool> TryRestoreDraftAsync()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (!localSettings.Values.TryGetValue(DraftStorageKey, out var rawDraft)
                || rawDraft is not string draftJson
                || string.IsNullOrWhiteSpace(draftJson))
            {
                return false;
            }

            AddProductDraft? draft;
            try
            {
                draft = JsonSerializer.Deserialize<AddProductDraft>(draftJson);
            }
            catch
            {
                await ClearDraftAsync(resetRestoredState: true);
                return false;
            }

            if (draft is null)
            {
                await ClearDraftAsync(resetRestoredState: true);
                return false;
            }

            await RestoreDraftAsync(draft);
            HasSavedDraft = true;
            IsDraftRestored = true;
            _isDraftDirty = false;
            return true;
        }

        private async Task RestoreDraftAsync(AddProductDraft draft)
        {
            _isRestoringDraft = true;
            try
            {
                Sku = draft.Sku;
                ProductName = draft.Name;
                ProductDescription = draft.Description;
                Specifications = draft.Specifications;
                ImportPriceValue = draft.ImportPrice;
                SellingPriceValue = draft.SellingPrice;
                StockQuantityValue = draft.StockQuantity;
                WarrantyMonthsValue = draft.WarrantyMonths;

                _suppressSeriesReload = true;
                SelectedBrandId = draft.SelectedBrandId ?? BrandOptions.FirstOrDefault()?.Id;
                _suppressSeriesReload = false;

                await LoadSeriesOptionsAsync();

                SelectedSeriesId = draft.SelectedSeriesId.HasValue &&
                                   SeriesOptions.Any(option => option.Id == draft.SelectedSeriesId.Value)
                    ? draft.SelectedSeriesId
                    : null;

                RestoreSelectedCategories(draft.SelectedCategoryIds);
                await RestoreImagesAsync(draft.ImageFilePaths);
            }
            finally
            {
                _isRestoringDraft = false;
            }
        }

        private void RestoreSelectedCategories(IReadOnlyCollection<int>? selectedCategoryIds)
        {
            var selectedIds = selectedCategoryIds ?? Array.Empty<int>();
            foreach (var option in CategoryOptions)
            {
                option.IsSelected = selectedIds.Contains(option.CategoryId);
            }
        }

        private async Task RestoreImagesAsync(IEnumerable<string>? imageFilePaths)
        {
            PreviewImages.Clear();
            MainPreview = new ImagePreview();

            if (imageFilePaths is null)
            {
                return;
            }

            foreach (var imagePath in imageFilePaths.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct())
            {
                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(imagePath);
                    var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    using var stream = await file.OpenReadAsync();
                    await bitmap.SetSourceAsync(stream);

                    AddImagePreview(new ImagePreview
                    {
                        Bitmap = bitmap,
                        File = file,
                    });
                }
                catch
                {
                    // Skip missing or inaccessible files during restore.
                }
            }
        }

        private Task SaveDraftIfDirtyAsync()
        {
            if (!_isDraftDirty || IsSaving || _isRestoringDraft)
            {
                return Task.CompletedTask;
            }

            try
            {
                var draft = BuildDraft();
                var serializedDraft = JsonSerializer.Serialize(draft);
                ApplicationData.Current.LocalSettings.Values[DraftStorageKey] = serializedDraft;

                HasSavedDraft = true;
                _isDraftDirty = false;
            }
            catch
            {
                // Ignore transient local persistence errors and keep the form usable.
            }

            return Task.CompletedTask;
        }

        private AddProductDraft BuildDraft()
        {
            return new AddProductDraft
            {
                Sku = Sku,
                Name = ProductName,
                Description = ProductDescription,
                Specifications = Specifications,
                ImportPrice = ImportPriceValue,
                SellingPrice = SellingPriceValue,
                StockQuantity = Convert.ToInt32(StockQuantityValue),
                WarrantyMonths = Convert.ToInt32(WarrantyMonthsValue),
                SelectedBrandId = SelectedBrandId,
                SelectedSeriesId = SelectedSeriesId,
                SelectedCategoryIds =
                [
                    .. CategoryOptions
                        .Where(option => option.IsSelected)
                        .Select(option => option.CategoryId)
                ],
                ImageFilePaths =
                [
                    .. PreviewImages
                        .Where(preview => preview.File is not null)
                        .Select(preview => preview.File!.Path)
                ]
            };
        }

        public async Task DiscardDraftAsync()
        {
            await ClearDraftAsync(resetRestoredState: true);
        }

        private Task ClearDraftAsync(bool resetRestoredState)
        {
            ApplicationData.Current.LocalSettings.Values.Remove(DraftStorageKey);
            HasSavedDraft = false;
            _isDraftDirty = false;

            if (resetRestoredState)
            {
                IsDraftRestored = false;
            }

            return Task.CompletedTask;
        }
    }

    public class NewCategoryInput
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
