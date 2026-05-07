using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Promotions.Dto;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Promotions
{
    public class PromotionsViewModel : ObservableObject
    {
        private readonly IPromotionService _promotionService;
        private CancellationTokenSource? _searchDebounceCts;
        private string _searchQuery = string.Empty;
        private bool _isLoading;
        private bool _isInitialized;
        private string _errorMessage = string.Empty;
        private int _currentPage = 1;
        private int _selectedPageSize = 10;
        private int _totalCount;

        public PromotionsViewModel(IPromotionService promotionService)
        {
            _promotionService = promotionService;

            PageSizeOptions.Add(5);
            PageSizeOptions.Add(10);
            PageSizeOptions.Add(15);
            PageSizeOptions.Add(20);

            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized && !IsLoading);
            AddPromotionCommand = new AsyncRelayCommand(AddPromotionAsync, () => !IsLoading);
            EditPromotionCommand = new AsyncRelayCommand<int>(EditPromotionAsync, _ => !IsLoading);
            DeletePromotionCommand = new AsyncRelayCommand<int>(DeletePromotionAsync, _ => !IsLoading);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => !IsLoading && CanGoPrevious);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => !IsLoading && CanGoNext);
            RefreshCommand = new AsyncRelayCommand(() => LoadPromotionsAsync(true), () => !IsLoading);
        }

        public ObservableCollection<PromotionListItemViewModel> Promotions { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [];

        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand AddPromotionCommand { get; }
        public IAsyncRelayCommand<int> EditPromotionCommand { get; }
        public IAsyncRelayCommand<int> DeletePromotionCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }

        public Func<PromotionEditorState, Task<PromotionEditorResult?>>? RequestPromotionEditorAsync { get; set; }
        public Func<PromotionListItemViewModel, Task<bool>>? ConfirmDeletePromotionAsync { get; set; }

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
                if (SetProperty(ref _errorMessage, value))
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

        public int SelectedPageSize
        {
            get => _selectedPageSize;
            set
            {
                if (SetProperty(ref _selectedPageSize, value))
                {
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
                foreach (var promotion in page.Items)
                {
                    var startDate = ParsePromotionDate(promotion.StartDate);
                    var endDate = ParsePromotionDate(promotion.EndDate);

                    Promotions.Add(new PromotionListItemViewModel(
                        promotion.PromotionId,
                        promotion.Code,
                        promotion.DiscountPercent,
                        promotion.DiscountAmount,
                        startDate,
                        endDate,
                        promotion.IsActive));
                }

                NotifyPagingChanged();
            }
            finally
            {
                IsLoading = false;
            }
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

        private void NotifyPagingChanged()
        {
            OnPropertyChanged(nameof(ResultText));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsEmpty));
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

            if (DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                    out var parsed))
            {
                return parsed;
            }

            if (DateTime.TryParse(value, out parsed))
            {
                return parsed;
            }

            return DateTime.Today;
        }
    }

    public class PromotionEditorState
    {
        public int? PromotionId { get; init; }
        public string Code { get; init; } = string.Empty;
        public double? DiscountPercent { get; init; }
        public double? DiscountAmount { get; init; }
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
            DateTime startDate,
            DateTime endDate,
            bool isActive) =>
            new()
            {
                PromotionId = promotionId,
                Code = code,
                DiscountPercent = discountPercent,
                DiscountAmount = discountAmount,
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
        public DateTimeOffset StartDate { get; init; }
        public DateTimeOffset EndDate { get; init; }
        public bool IsActive { get; init; }
    }
}
