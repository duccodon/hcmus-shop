using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Promotions.Dto;
using hcmus_shop.ViewModels.Common;
using hcmus_shop.ViewModels.Products;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Promotions
{
    public class PromotionsViewModel : ObservableObject
    {
        private const int DefaultPageSize = 10;
        private readonly IPromotionService _promotionService;
        private readonly IAuthService _authService;
        private readonly ISettingsService _settingsService;
        private CancellationTokenSource? _searchDebounceCts;
        private string _searchQuery = string.Empty;
        private bool _isLoading;
        private bool _isInitialized;
        private string _errorMessage = string.Empty;
        private int _currentPage = 1;
        private int _selectedPageSize = 10;
        private int _totalCount;
        private PromotionTableOption? _selectedSortField;
        private PromotionTableOption? _selectedSortDirection;
        private string _selectedStatusFilter = "All";

        public PromotionsViewModel(IPromotionService promotionService, IAuthService authService, ISettingsService settingsService)
        {
            _promotionService = promotionService;
            _authService = authService;
            _settingsService = settingsService;
            _selectedPageSize = NormalizePageSize(_settingsService.PageSize);
            if (_settingsService.PageSize != _selectedPageSize)
            {
                _settingsService.PageSize = _selectedPageSize;
            }
            _settingsService.SettingsChanged += OnSettingsChanged;

            PageSizeOptions.Add(5);
            PageSizeOptions.Add(10);
            PageSizeOptions.Add(15);
            PageSizeOptions.Add(20);
            RankOptions.Add("All ranks");
            RankOptions.Add("Bronze");
            RankOptions.Add("Silver");
            RankOptions.Add("Gold");
            RankOptions.Add("Diamond");
            SortFieldOptions.Add(new PromotionTableOption("createdAt", "Created"));
            SortFieldOptions.Add(new PromotionTableOption("code", "Code"));
            SortFieldOptions.Add(new PromotionTableOption("discount", "Discount"));
            SortFieldOptions.Add(new PromotionTableOption("rank", "Eligible Rank"));
            SortFieldOptions.Add(new PromotionTableOption("startDate", "Start Date"));
            SortFieldOptions.Add(new PromotionTableOption("endDate", "End Date"));
            SortDirectionOptions.Add(new PromotionTableOption("asc", "Ascending"));
            SortDirectionOptions.Add(new PromotionTableOption("desc", "Descending"));
            StatusFilterOptions.Add("All");
            StatusFilterOptions.Add("Active");
            StatusFilterOptions.Add("Scheduled");
            StatusFilterOptions.Add("Expired");
            StatusFilterOptions.Add("Inactive");
            SelectedSortField = SortFieldOptions.FirstOrDefault(option => option.Key == "createdAt") ?? SortFieldOptions.FirstOrDefault();
            SelectedSortDirection = SortDirectionOptions.FirstOrDefault(option => option.Key == "desc") ?? SortDirectionOptions.FirstOrDefault();

            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized && !IsLoading);
            AddPromotionCommand = new AsyncRelayCommand(AddPromotionAsync, () => CanManagePromotions && !IsLoading);
            EditPromotionCommand = new AsyncRelayCommand<int>(EditPromotionAsync, _ => CanManagePromotions && !IsLoading);
            DeletePromotionCommand = new AsyncRelayCommand<int>(DeletePromotionAsync, _ => CanManagePromotions && !IsLoading);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => !IsLoading && CanGoPrevious);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => !IsLoading && CanGoNext);
            GoToPageCommand = new AsyncRelayCommand<int>(GoToPageAsync);
            RefreshCommand = new AsyncRelayCommand(() => LoadPromotionsAsync(true), () => !IsLoading);
        }

        public ObservableCollection<PromotionListItemViewModel> Promotions { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [];
        public ObservableCollection<PageButtonItem> PageButtons { get; } = [];
        public ObservableCollection<string> RankOptions { get; } = [];
        public ObservableCollection<PromotionTableOption> SortFieldOptions { get; } = [];
        public ObservableCollection<PromotionTableOption> SortDirectionOptions { get; } = [];
        public ObservableCollection<string> StatusFilterOptions { get; } = [];

        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand AddPromotionCommand { get; }
        public IAsyncRelayCommand<int> EditPromotionCommand { get; }
        public IAsyncRelayCommand<int> DeletePromotionCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand<int> GoToPageCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }

        public Func<PromotionEditorState, Task<PromotionEditorResult?>>? RequestPromotionEditorAsync { get; set; }
        public Func<PromotionListItemViewModel, Task<bool>>? ConfirmDeletePromotionAsync { get; set; }
        public bool CanManagePromotions => _authService.HasRole("Admin");

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

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    InitializeCommand.NotifyCanExecuteChanged();
                    AddPromotionCommand.NotifyCanExecuteChanged();
                    EditPromotionCommand.NotifyCanExecuteChanged();
                    DeletePromotionCommand.NotifyCanExecuteChanged();
                    PreviousPageCommand.NotifyCanExecuteChanged();
                    NextPageCommand.NotifyCanExecuteChanged();
                    RefreshCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                var formattedValue = UserErrorMessageFormatter.Format(value);
                if (SetProperty(ref _errorMessage, formattedValue))
                {
                    OnPropertyChanged(nameof(HasError));
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool IsEmpty => !IsLoading && !HasError && Promotions.Count == 0;

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _currentPage = 1;
                    DebounceSearch();
                }
            }
        }

        public PromotionTableOption? SelectedSortField
        {
            get => _selectedSortField;
            set
            {
                if (SetProperty(ref _selectedSortField, value) && IsInitialized)
                {
                    _currentPage = 1;
                    _ = LoadPromotionsAsync();
                }
            }
        }

        public PromotionTableOption? SelectedSortDirection
        {
            get => _selectedSortDirection;
            set
            {
                if (SetProperty(ref _selectedSortDirection, value) && IsInitialized)
                {
                    _currentPage = 1;
                    _ = LoadPromotionsAsync();
                }
            }
        }

        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (SetProperty(ref _selectedStatusFilter, value) && IsInitialized)
                {
                    _currentPage = 1;
                    _ = LoadPromotionsAsync();
                }
            }
        }

        public int SelectedPageSize
        {
            get => _selectedPageSize;
            set
            {
                var normalizedValue = NormalizePageSize(value);
                if (SetProperty(ref _selectedPageSize, normalizedValue))
                {
                    _settingsService.PageSize = normalizedValue;
                    _currentPage = 1;
                    _ = LoadPromotionsAsync();
                }
            }
        }

        public bool CanGoPrevious => _currentPage > 1;
        public bool CanGoNext => _currentPage < TotalPages;

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

        private int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Math.Max(_totalCount, 1) / SelectedPageSize));

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            await LoadPromotionsAsync();
            if (!HasError)
            {
                IsInitialized = true;
            }
        }

        private async Task AddPromotionAsync()
        {
            if (RequestPromotionEditorAsync is null)
            {
                ErrorMessage = "Promotion editor is not available.";
                return;
            }

            ErrorMessage = string.Empty;
            var draft = await RequestPromotionEditorAsync(PromotionEditorState.Create());
            if (draft is null)
            {
                return;
            }

            if (!TryValidateEditorInput(draft, out var validationError))
            {
                ErrorMessage = validationError;
                return;
            }

            var result = await _promotionService.CreateAsync(new CreatePromotionInput
            {
                Code = draft.Code.Trim(),
                DiscountPercent = draft.DiscountPercent,
                DiscountAmount = draft.DiscountAmount,
                MinimumCustomerRank = NormalizeMinimumRank(draft.MinimumCustomerRank),
                StartDate = draft.StartDate.ToString("O"),
                EndDate = draft.EndDate.ToString("O"),
                IsActive = draft.IsActive
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to create promotion.";
                return;
            }

            await LoadPromotionsAsync();
        }

        private async Task EditPromotionAsync(int promotionId)
        {
            if (promotionId <= 0 || RequestPromotionEditorAsync is null)
            {
                return;
            }

            ErrorMessage = string.Empty;
            var current = await _promotionService.GetByIdAsync(promotionId);
            if (!current.IsSuccess)
            {
                ErrorMessage = current.Error ?? "Failed to load promotion.";
                return;
            }

            if (current.Value is null)
            {
                ErrorMessage = "Promotion not found.";
                return;
            }

            var promotion = current.Value;
            var startDate = ParsePromotionDate(promotion.StartDate);
            var endDate = ParsePromotionDate(promotion.EndDate);
            var draft = await RequestPromotionEditorAsync(PromotionEditorState.Edit(
                promotion.PromotionId,
                promotion.Code,
                promotion.DiscountPercent,
                promotion.DiscountAmount,
                promotion.MinimumCustomerRank,
                startDate,
                endDate,
                promotion.IsActive));

            if (draft is null)
            {
                return;
            }

            if (!TryValidateEditorInput(draft, out var validationError))
            {
                ErrorMessage = validationError;
                return;
            }

            var result = await _promotionService.UpdateAsync(promotionId, new UpdatePromotionInput
            {
                Code = draft.Code.Trim(),
                DiscountPercent = draft.DiscountPercent,
                DiscountAmount = draft.DiscountAmount,
                MinimumCustomerRank = NormalizeMinimumRank(draft.MinimumCustomerRank),
                StartDate = draft.StartDate.ToString("O"),
                EndDate = draft.EndDate.ToString("O"),
                IsActive = draft.IsActive
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to update promotion.";
                return;
            }

            await LoadPromotionsAsync();
        }

        private async Task DeletePromotionAsync(int promotionId)
        {
            if (promotionId <= 0)
            {
                return;
            }

            var target = Promotions.FirstOrDefault(item => item.PromotionId == promotionId);
            if (target is null)
            {
                return;
            }

            if (ConfirmDeletePromotionAsync is not null)
            {
                var confirmed = await ConfirmDeletePromotionAsync(target);
                if (!confirmed)
                {
                    return;
                }
            }

            ErrorMessage = string.Empty;
            var result = await _promotionService.DeleteAsync(promotionId);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to deactivate promotion.";
                return;
            }

            if (Promotions.Count <= 1 && _currentPage > 1)
            {
                _currentPage--;
            }

            await LoadPromotionsAsync();
        }

        private async Task PreviousPageAsync()
        {
            if (!CanGoPrevious)
            {
                return;
            }

            _currentPage--;
            await LoadPromotionsAsync();
        }

        private async Task NextPageAsync()
        {
            if (!CanGoNext)
            {
                return;
            }

            _currentPage++;
            await LoadPromotionsAsync();
        }

        private async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPages || page == _currentPage)
            {
                return;
            }

            _currentPage = page;
            await LoadPromotionsAsync();
        }

        public async Task LoadPromotionsAsync(bool clearError = true)
        {
            IsLoading = true;
            if (clearError)
            {
                ErrorMessage = string.Empty;
            }

            try
            {
                var result = await _promotionService.GetAllAsync(new PromotionFilterDto
                {
                    Search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                    Page = _currentPage,
                    PageSize = SelectedPageSize
                });

                if (!result.IsSuccess || result.Value is null)
                {
                    Promotions.Clear();
                    _totalCount = 0;
                    RebuildPageButtons();
                    ErrorMessage = result.Error ?? "Failed to load promotions.";
                    NotifyPagingChanged();
                    return;
                }

                var page = result.Value;
                _totalCount = page.TotalCount;
                if (_currentPage > TotalPages)
                {
                    _currentPage = TotalPages;
                }

                Promotions.Clear();
                foreach (var promotion in ApplyPromotionTableOperations(page.Items))
                {
                    var startDate = ParsePromotionDate(promotion.StartDate);
                    var endDate = ParsePromotionDate(promotion.EndDate);

                    Promotions.Add(new PromotionListItemViewModel(
                        promotion.PromotionId,
                        promotion.Code,
                        promotion.DiscountPercent,
                        promotion.DiscountAmount,
                        promotion.MinimumCustomerRank,
                        startDate,
                        endDate,
                        promotion.IsActive));
                }

                RebuildPageButtons();
                NotifyPagingChanged();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private IEnumerable<PromotionDto> ApplyPromotionTableOperations(IEnumerable<PromotionDto> items)
        {
            IEnumerable<PromotionDto> query = items;

            query = SelectedStatusFilter switch
            {
                "Active" => query.Where(item => GetPromotionStatus(item) == "Active"),
                "Scheduled" => query.Where(item => GetPromotionStatus(item) == "Scheduled"),
                "Expired" => query.Where(item => GetPromotionStatus(item) == "Expired"),
                "Inactive" => query.Where(item => GetPromotionStatus(item) == "Inactive"),
                _ => query
            };

            var descending = string.Equals(SelectedSortDirection?.Key, "desc", StringComparison.OrdinalIgnoreCase);
            query = (SelectedSortField?.Key ?? "createdAt") switch
            {
                "createdAt" => descending
                    ? query.OrderByDescending(item => ParsePromotionDate(item.CreatedAt))
                    : query.OrderBy(item => ParsePromotionDate(item.CreatedAt)),
                "discount" => descending
                    ? query.OrderByDescending(item => item.DiscountPercent ?? item.DiscountAmount ?? 0)
                    : query.OrderBy(item => item.DiscountPercent ?? item.DiscountAmount ?? 0),
                "rank" => descending
                    ? query.OrderByDescending(item => item.MinimumCustomerRank)
                    : query.OrderBy(item => item.MinimumCustomerRank),
                "startDate" => descending
                    ? query.OrderByDescending(item => ParsePromotionDate(item.StartDate))
                    : query.OrderBy(item => ParsePromotionDate(item.StartDate)),
                "endDate" => descending
                    ? query.OrderByDescending(item => ParsePromotionDate(item.EndDate))
                    : query.OrderBy(item => ParsePromotionDate(item.EndDate)),
                _ => descending ? query.OrderByDescending(item => item.Code) : query.OrderBy(item => item.Code),
            };

            return query;
        }

        private string GetPromotionStatus(PromotionDto promotion)
        {
            if (!promotion.IsActive)
            {
                return "Inactive";
            }

            var startDate = ParsePromotionDate(promotion.StartDate).ToUniversalTime();
            var endDate = ParsePromotionDate(promotion.EndDate).ToUniversalTime();
            var now = DateTime.UtcNow;

            if (startDate > now)
            {
                return "Scheduled";
            }

            if (endDate < now)
            {
                return "Expired";
            }

            return "Active";
        }

        private void DebounceSearch()
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts?.Dispose();
            _searchDebounceCts = new CancellationTokenSource();
            _ = DebounceSearchAsync(_searchDebounceCts.Token);
        }

        private async Task DebounceSearchAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(400, token);
                if (!token.IsCancellationRequested)
                {
                    await LoadPromotionsAsync();
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            var normalizedValue = NormalizePageSize(_settingsService.PageSize);
            if (_selectedPageSize == normalizedValue)
            {
                return;
            }

            _selectedPageSize = normalizedValue;
            OnPropertyChanged(nameof(SelectedPageSize));
            OnPropertyChanged(nameof(ResultText));
            _currentPage = 1;
            if (IsInitialized)
            {
                _ = LoadPromotionsAsync();
            }
        }

        private static int NormalizePageSize(int value)
        {
            return value is 5 or 10 or 15 or 20 ? value : DefaultPageSize;
        }

        private void NotifyPagingChanged()
        {
            OnPropertyChanged(nameof(ResultText));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void RebuildPageButtons()
        {
            PageButtons.Clear();

            PageButtons.Add(new PageButtonItem
            {
                Label = "<- Previous",
                PageNumber = _currentPage - 1,
                IsEnabled = _currentPage > 1,
                IsCurrent = false
            });

            int? previousPage = null;
            foreach (var page in BuildPageNumbers(_currentPage, TotalPages))
            {
                if (previousPage.HasValue && page - previousPage.Value > 1)
                {
                    PageButtons.Add(new PageButtonItem
                    {
                        Label = "...",
                        PageNumber = -1,
                        IsEnabled = false,
                        IsCurrent = false
                    });
                }

                PageButtons.Add(new PageButtonItem
                {
                    Label = page.ToString(),
                    PageNumber = page,
                    IsEnabled = page != _currentPage,
                    IsCurrent = page == _currentPage
                });
                previousPage = page;
            }

            PageButtons.Add(new PageButtonItem
            {
                Label = "Next ->",
                PageNumber = _currentPage + 1,
                IsEnabled = _currentPage < TotalPages,
                IsCurrent = false
            });
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

        private static DateTime ParsePromotionDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.Today;
            }

            if (DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                    out var parsedOffset))
            {
                return parsedOffset.LocalDateTime;
            }

            if (DateTime.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return DateTime.Today;
        }

        private static string? NormalizeMinimumRank(string? rank)
        {
            return string.Equals(rank, "All ranks", StringComparison.OrdinalIgnoreCase)
                ? null
                : rank;
        }
    }

    public class PromotionEditorState
    {
        public int? PromotionId { get; init; }
        public string Code { get; init; } = string.Empty;
        public double? DiscountPercent { get; init; }
        public double? DiscountAmount { get; init; }
        public string MinimumCustomerRank { get; init; } = "All ranks";
        public DateTimeOffset StartDate { get; init; } = DateTimeOffset.Now;
        public DateTimeOffset EndDate { get; init; } = DateTimeOffset.Now.AddDays(30);
        public bool IsActive { get; init; } = true;
        public bool IsEditMode => PromotionId.HasValue;

        public static PromotionEditorState Create() => new();

        public static PromotionEditorState Edit(
            int promotionId,
            string code,
            double? discountPercent,
            double? discountAmount,
            string? minimumCustomerRank,
            DateTime startDate,
            DateTime endDate,
            bool isActive) =>
            new()
            {
                PromotionId = promotionId,
                Code = code,
                DiscountPercent = discountPercent,
                DiscountAmount = discountAmount,
                MinimumCustomerRank = string.IsNullOrWhiteSpace(minimumCustomerRank) ? "All ranks" : minimumCustomerRank,
                StartDate = new DateTimeOffset(startDate),
                EndDate = new DateTimeOffset(endDate),
                IsActive = isActive
            };
    }

    public class PromotionEditorResult
    {
        public string Code { get; init; } = string.Empty;
        public double? DiscountPercent { get; init; }
        public double? DiscountAmount { get; init; }
        public string? MinimumCustomerRank { get; init; }
        public DateTimeOffset StartDate { get; init; }
        public DateTimeOffset EndDate { get; init; }
        public bool IsActive { get; init; }
    }

    public class PromotionTableOption
    {
        public PromotionTableOption(string key, string label)
        {
            Key = key;
            Label = label;
        }

        public string Key { get; }
        public string Label { get; }
    }
}
