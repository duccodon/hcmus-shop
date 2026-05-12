using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Customers.Dto;
using hcmus_shop.Services.Orders.Dto;
using hcmus_shop.ViewModels.Customers;
using hcmus_shop.ViewModels.Products;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace hcmus_shop.ViewModels.Orders
{
    public class OrdersViewModel : ObservableObject
    {
        private const string DraftStorageKey = "CreateOrderDraft";
        private const int DefaultPageSize = 10;

        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IPromotionService _promotionService;
        private readonly IInvoiceService _invoiceService;
        private readonly IAuthService _authService;
        private readonly IConfigService _configService;
        private readonly ISettingsService _settingsService;
        private readonly DispatcherTimer _autoSaveTimer;
        private readonly List<ProductInstanceDto> _availableInstancesCache = [];
        private CancellationTokenSource? _searchDebounceCts;
        private CancellationTokenSource? _instanceDebounceCts;

        private bool _isInitialized;
        private bool _isLoading;
        private bool _isEditorBusy;
        private string _errorMessage = string.Empty;
        private string _searchQuery = string.Empty;
        private string _selectedStatus = string.Empty;
        private DateTimeOffset _fromDate = DateTimeOffset.Now.AddMonths(-1);
        private DateTimeOffset _toDate = DateTimeOffset.Now;
        private int _selectedPageSize = 10;
        private int _currentPage = 1;
        private int _totalCount;
        private OrderDto? _selectedOrder;
        private bool _isEditorOpen;
        private bool _isEditMode;
        private string _editorCustomerId = string.Empty;
        private string _editorPromotionCode = string.Empty;
        private string _editorNotes = string.Empty;
        private string _editorErrorMessage = string.Empty;
        private string _editorStatusMessage = string.Empty;
        private string _instanceSearchQuery = string.Empty;
        private PromotionValidationDto? _appliedPromotion;
        private OrderTableOption? _selectedSortField;
        private OrderTableOption? _selectedSortDirection;
        private bool _isDraftDirty;
        private bool _isRestoringDraft;
        private bool _hasSavedDraft;
        private bool _isDraftRestored;
        private bool _isAutoSaveActive;
        private string _draftEventMessage = string.Empty;

        public OrdersViewModel(
            IOrderService orderService,
            ICustomerService customerService,
            IPromotionService promotionService,
            IInvoiceService invoiceService,
            IAuthService authService,
            IConfigService configService,
            ISettingsService settingsService)
        {
            _orderService = orderService;
            _customerService = customerService;
            _promotionService = promotionService;
            _invoiceService = invoiceService;
            _authService = authService;
            _configService = configService;
            _settingsService = settingsService;
            _selectedPageSize = NormalizePageSize(_settingsService.PageSize);
            if (_settingsService.PageSize != _selectedPageSize)
            {
                _settingsService.PageSize = _selectedPageSize;
            }
            _settingsService.SettingsChanged += OnSettingsChanged;

            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;

            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized && !IsLoading);
            RefreshCommand = new AsyncRelayCommand(LoadOrdersAsync, () => !IsLoading);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => !IsLoading && CurrentPage > 1);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => !IsLoading && CurrentPage < TotalPages);
            GoToPageCommand = new AsyncRelayCommand<int>(GoToPageAsync);
            BeginCreateOrderCommand = new AsyncRelayCommand(BeginCreateOrderAsync, () => !IsLoading && !IsEditorBusy);
            BeginEditOrderCommand = new AsyncRelayCommand(BeginEditOrderAsync, () => CanModifySelectedCreatedOrder);
            SaveOrderCommand = new AsyncRelayCommand(SaveOrderAsync, () => IsEditorOpen && !IsEditorBusy);
            CancelEditorCommand = new RelayCommand(CancelEditor);
            DiscardDraftCommand = new AsyncRelayCommand(DiscardDraftAsync, () => IsEditorOpen && !IsEditMode);
            ApplyPromotionCommand = new AsyncRelayCommand(ApplyPromotionAsync, () => IsEditorOpen && !IsEditorBusy);
            MarkPaidCommand = new AsyncRelayCommand(MarkPaidAsync, () => CanModifySelectedCreatedOrder);
            CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync, () => CanModifySelectedCreatedOrder);
            DeleteOrderCommand = new AsyncRelayCommand(DeleteOrderAsync, () => CanDeleteSelectedOrder);
            PrintInvoiceCommand = new AsyncRelayCommand(PrintInvoiceAsync, () => SelectedOrder is not null && !IsLoading);
            RemoveCartItemCommand = new RelayCommand<OrderCartItemViewModel?>(RemoveCartItem);
            IncreaseQuantityCommand = new RelayCommand<OrderCartItemViewModel?>(IncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand<OrderCartItemViewModel?>(DecreaseQuantity);
            CreateInlineCustomerCommand = new AsyncRelayCommand(CreateInlineCustomerAsync, () => IsEditorOpen && !IsEditorBusy);

            SelectedStatusOptions.Add(string.Empty);
            SelectedStatusOptions.Add("Created");
            SelectedStatusOptions.Add("Paid");
            SelectedStatusOptions.Add("Cancelled");

            SortFieldOptions.Add(new OrderTableOption("customer", "Customer"));
            SortFieldOptions.Add(new OrderTableOption("createdAt", "Created"));
            SortFieldOptions.Add(new OrderTableOption("amount", "Amount"));

            SortDirectionOptions.Add(new OrderTableOption("asc", "Ascending"));
            SortDirectionOptions.Add(new OrderTableOption("desc", "Descending"));

            SelectedSortField = SortFieldOptions.FirstOrDefault(option => option.Key == "createdAt") ?? SortFieldOptions.FirstOrDefault();
            SelectedSortDirection = SortDirectionOptions.FirstOrDefault(option => option.Key == "desc") ?? SortDirectionOptions.FirstOrDefault();
        }

        public ObservableCollection<OrderDto> Orders { get; } = [];
        public ObservableCollection<CustomerDto> Customers { get; } = [];
        public ObservableCollection<OrderProductSuggestionViewModel> ProductSuggestions { get; } = [];
        public ObservableCollection<OrderCartItemViewModel> CartItems { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [5, 10, 15, 20];
        public ObservableCollection<PageButtonItem> PageButtons { get; } = [];
        public ObservableCollection<string> SelectedStatusOptions { get; } = [];
        public ObservableCollection<OrderTableOption> SortFieldOptions { get; } = [];
        public ObservableCollection<OrderTableOption> SortDirectionOptions { get; } = [];

        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand<int> GoToPageCommand { get; }
        public IAsyncRelayCommand BeginCreateOrderCommand { get; }
        public IAsyncRelayCommand BeginEditOrderCommand { get; }
        public IAsyncRelayCommand SaveOrderCommand { get; }
        public IRelayCommand CancelEditorCommand { get; }
        public IAsyncRelayCommand DiscardDraftCommand { get; }
        public IAsyncRelayCommand ApplyPromotionCommand { get; }
        public IAsyncRelayCommand MarkPaidCommand { get; }
        public IAsyncRelayCommand CancelOrderCommand { get; }
        public IAsyncRelayCommand DeleteOrderCommand { get; }
        public IAsyncRelayCommand PrintInvoiceCommand { get; }
        public IRelayCommand<OrderCartItemViewModel?> RemoveCartItemCommand { get; }
        public IRelayCommand<OrderCartItemViewModel?> IncreaseQuantityCommand { get; }
        public IRelayCommand<OrderCartItemViewModel?> DecreaseQuantityCommand { get; }
        public IAsyncRelayCommand CreateInlineCustomerCommand { get; }

        public Func<string, Task<string?>>? RequestInvoicePathAsync { get; set; }
        public Func<OrderDto, Task<bool>>? ConfirmDeleteOrderAsync { get; set; }
        public Func<CustomerEditorState, Task<CustomerEditorResult?>>? RequestCustomerEditorAsync { get; set; }

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
                    RefreshCommand.NotifyCanExecuteChanged();
                    PreviousPageCommand.NotifyCanExecuteChanged();
                    NextPageCommand.NotifyCanExecuteChanged();
                    BeginCreateOrderCommand.NotifyCanExecuteChanged();
                    BeginEditOrderCommand.NotifyCanExecuteChanged();
                    MarkPaidCommand.NotifyCanExecuteChanged();
                    CancelOrderCommand.NotifyCanExecuteChanged();
                    DeleteOrderCommand.NotifyCanExecuteChanged();
                    PrintInvoiceCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IsEmpty));
                    OnPropertyChanged(nameof(CanDeleteSelectedOrder));
                }
            }
        }

        public bool IsEditorBusy
        {
            get => _isEditorBusy;
            private set
            {
                if (SetProperty(ref _isEditorBusy, value))
                {
                    BeginCreateOrderCommand.NotifyCanExecuteChanged();
                    SaveOrderCommand.NotifyCanExecuteChanged();
                    ApplyPromotionCommand.NotifyCanExecuteChanged();
                    CreateInlineCustomerCommand.NotifyCanExecuteChanged();
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
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value) && IsInitialized)
                {
                    _currentPage = 1;
                    DebounceSearch();
                }
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value) && IsInitialized)
                {
                    _currentPage = 1;
                    _ = LoadOrdersAsync();
                }
            }
        }

        public OrderTableOption? SelectedSortField
        {
            get => _selectedSortField;
            set
            {
                if (SetProperty(ref _selectedSortField, value) && IsInitialized)
                {
                    _currentPage = 1;
                    _ = LoadOrdersAsync();
                }
            }
        }

        public OrderTableOption? SelectedSortDirection
        {
            get => _selectedSortDirection;
            set
            {
                if (SetProperty(ref _selectedSortDirection, value) && IsInitialized)
                {
                    _currentPage = 1;
                    _ = LoadOrdersAsync();
                }
            }
        }

        public DateTimeOffset FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value) && IsInitialized)
                {
                    _currentPage = 1;
                    _ = LoadOrdersAsync();
                }
            }
        }

        public DateTimeOffset ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value) && IsInitialized)
                {
                    _currentPage = 1;
                    _ = LoadOrdersAsync();
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
                    _ = LoadOrdersAsync();
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    PreviousPageCommand.NotifyCanExecuteChanged();
                    NextPageCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(ResultText));
                }
            }
        }

        public OrderDto? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (SetProperty(ref _selectedOrder, value))
                {
                    BeginEditOrderCommand.NotifyCanExecuteChanged();
                    MarkPaidCommand.NotifyCanExecuteChanged();
                    CancelOrderCommand.NotifyCanExecuteChanged();
                    DeleteOrderCommand.NotifyCanExecuteChanged();
                    PrintInvoiceCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(SelectedOrderSummary));
                    OnPropertyChanged(nameof(SelectedOrderCustomerName));
                    OnPropertyChanged(nameof(SelectedOrderCustomerPhone));
                    OnPropertyChanged(nameof(SelectedOrderCustomerEmail));
                    OnPropertyChanged(nameof(SelectedOrderSubtotalDisplay));
                    OnPropertyChanged(nameof(SelectedOrderDiscountDisplay));
                    OnPropertyChanged(nameof(SelectedOrderFinalAmountDisplay));
                    OnPropertyChanged(nameof(SelectedOrderPromotionCode));
                    OnPropertyChanged(nameof(SelectedOrderNotesDisplay));
                    OnPropertyChanged(nameof(SelectedOrderDetailItems));
                    OnPropertyChanged(nameof(CanDeleteSelectedOrder));
                }
            }
        }

        public bool IsEditorOpen
        {
            get => _isEditorOpen;
            private set
            {
                if (SetProperty(ref _isEditorOpen, value))
                {
                    SaveOrderCommand.NotifyCanExecuteChanged();
                    ApplyPromotionCommand.NotifyCanExecuteChanged();
                    CreateInlineCustomerCommand.NotifyCanExecuteChanged();
                    DiscardDraftCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(EditorTitle));
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            private set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    OnPropertyChanged(nameof(EditorTitle));
                    DiscardDraftCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string EditorTitle => IsEditMode ? "Edit Order" : "Create Order";

        public string EditorCustomerId
        {
            get => _editorCustomerId;
            set
            {
                if (SetProperty(ref _editorCustomerId, value))
                {
                    MarkDraftDirty();
                }
            }
        }

        public string EditorPromotionCode
        {
            get => _editorPromotionCode;
            set
            {
                if (SetProperty(ref _editorPromotionCode, value))
                {
                    MarkDraftDirty();
                }
            }
        }

        public string EditorNotes
        {
            get => _editorNotes;
            set
            {
                if (SetProperty(ref _editorNotes, value))
                {
                    MarkDraftDirty();
                }
            }
        }

        public string EditorErrorMessage
        {
            get => _editorErrorMessage;
            private set
            {
                if (SetProperty(ref _editorErrorMessage, value))
                {
                    OnPropertyChanged(nameof(HasEditorError));
                }
            }
        }

        public bool HasEditorError => !string.IsNullOrWhiteSpace(EditorErrorMessage);

        public string EditorStatusMessage
        {
            get => _editorStatusMessage;
            private set => SetProperty(ref _editorStatusMessage, value);
        }

        public string InstanceSearchQuery
        {
            get => _instanceSearchQuery;
            set
            {
                if (SetProperty(ref _instanceSearchQuery, value) && IsEditorOpen)
                {
                    DebounceInstanceSearch();
                }
            }
        }

        public bool HasSavedDraft
        {
            get => _hasSavedDraft;
            private set
            {
                if (SetProperty(ref _hasSavedDraft, value))
                {
                    OnPropertyChanged(nameof(AutoSaveStatusText));
                    OnPropertyChanged(nameof(HasAutoSaveStatusText));
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
                    OnPropertyChanged(nameof(AutoSaveStatusText));
                    OnPropertyChanged(nameof(HasAutoSaveStatusText));
                }
            }
        }

        public bool IsAutoSaveActive
        {
            get => _isAutoSaveActive;
            private set
            {
                if (SetProperty(ref _isAutoSaveActive, value))
                {
                    OnPropertyChanged(nameof(AutoSaveStatusText));
                    OnPropertyChanged(nameof(HasAutoSaveStatusText));
                }
            }
        }

        public string DraftEventMessage
        {
            get => _draftEventMessage;
            private set
            {
                if (SetProperty(ref _draftEventMessage, value))
                {
                    OnPropertyChanged(nameof(HasDraftEventMessage));
                    OnPropertyChanged(nameof(AutoSaveStatusText));
                    OnPropertyChanged(nameof(HasAutoSaveStatusText));
                }
            }
        }

        public bool HasDraftEventMessage => !string.IsNullOrWhiteSpace(DraftEventMessage);

        public string AutoSaveStatusText =>
            HasDraftEventMessage ? DraftEventMessage :
            IsDraftRestored ? "Draft restored." :
            IsAutoSaveActive ? "Auto-save on." :
            string.Empty;

        public bool HasAutoSaveStatusText => !string.IsNullOrWhiteSpace(AutoSaveStatusText);

        public bool IsEmpty => !IsLoading && Orders.Count == 0 && !HasError;

        public string ResultText =>
            _totalCount == 0
                ? "0 orders"
                : $"{((_currentPage - 1) * SelectedPageSize) + 1}-{Math.Min(_currentPage * SelectedPageSize, _totalCount)} of {_totalCount}";

        public string SelectedOrderSummary =>
            SelectedOrder is null
                ? "Select an order to view details."
                : $"{SelectedOrder.Status} | {FormatCurrency(SelectedOrder.FinalAmount)} | {FormatDate(SelectedOrder.CreatedAt)}";

        public string SelectedOrderCustomerName => SelectedOrder?.Customer?.Name ?? "No customer selected";
        public string SelectedOrderCustomerPhone => SelectedOrder?.Customer?.Phone ?? string.Empty;
        public string SelectedOrderCustomerEmail => SelectedOrder?.Customer?.Email ?? string.Empty;
        public string SelectedOrderSubtotalDisplay => FormatCurrency(SelectedOrder?.Subtotal ?? 0);
        public string SelectedOrderDiscountDisplay => FormatCurrency(SelectedOrder?.DiscountAmount ?? 0);
        public string SelectedOrderFinalAmountDisplay => FormatCurrency(SelectedOrder?.FinalAmount ?? 0);
        public string SelectedOrderPromotionCode => SelectedOrder?.Promotion?.Code ?? "No promotion";
        public string SelectedOrderNotesDisplay => string.IsNullOrWhiteSpace(SelectedOrder?.Notes) ? "No notes provided." : SelectedOrder!.Notes!;
        public IReadOnlyList<OrderItemDto> SelectedOrderDetailItems => SelectedOrder?.OrderItems ?? [];

        public string PromotionSummary =>
            _appliedPromotion?.IsValid == true && _appliedPromotion.Promotion is not null
                ? $"Applied: {_appliedPromotion.Promotion.Code}"
                : "No promotion applied";

        public double EditorSubtotal => CartItems.Sum(item => item.LineTotal);

        public double EditorDiscountAmount
        {
            get
            {
                if (_appliedPromotion?.IsValid != true || _appliedPromotion.Promotion is null)
                {
                    return 0;
                }

                var promotion = _appliedPromotion.Promotion;
                if (promotion.DiscountPercent.HasValue)
                {
                    return Math.Min(EditorSubtotal * promotion.DiscountPercent.Value / 100d, EditorSubtotal);
                }

                return Math.Min(promotion.DiscountAmount ?? 0, EditorSubtotal);
            }
        }

        public double EditorFinalAmount => Math.Max(EditorSubtotal - EditorDiscountAmount, 0);
        public bool HasSelectedOrderItems => CartItems.Count > 0;
        public int SelectedItemCount => CartItems.Sum(item => item.Quantity);
        public string CartSummaryText => $"{SelectedItemCount} sản phẩm";

        public bool CanModifySelectedCreatedOrder =>
            SelectedOrder is not null &&
            string.Equals(SelectedOrder.Status, "Created", StringComparison.OrdinalIgnoreCase) &&
            !IsLoading;

        public bool CanDeleteSelectedOrder => CanModifySelectedCreatedOrder && _authService.HasRole("Admin");

        private int TotalPages => Math.Max(1, (int)Math.Ceiling(Math.Max(_totalCount, 1) / (double)SelectedPageSize));

        public async Task PersistDraftAsync()
        {
            await SaveDraftIfDirtyAsync();
        }

        private async void AutoSaveTimer_Tick(object? sender, object e)
        {
            await SaveDraftIfDirtyAsync();
        }

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            await LoadCustomersAsync();
            await LoadOrdersAsync();
            IsInitialized = true;
        }

        private async Task LoadCustomersAsync()
        {
            var result = await _customerService.GetAllAsync(new CustomerFilterDto
            {
                Page = 1,
                PageSize = 200
            });

            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to load customers.";
                return;
            }

            Customers.Clear();
            foreach (var customer in result.Value.Items.OrderByDescending(customer => ParseDateTime(customer.CreatedAt)))
            {
                Customers.Add(customer);
            }
        }

        private async Task LoadOrdersAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _orderService.GetAllAsync(new OrderFilterDto
                {
                    Status = string.IsNullOrWhiteSpace(SelectedStatus) ? null : SelectedStatus,
                    Search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                    FromDate = FromDate.ToString("yyyy-MM-dd"),
                    ToDate = ToDate.ToString("yyyy-MM-dd"),
                    Page = CurrentPage,
                    PageSize = SelectedPageSize
                });

                if (!result.IsSuccess || result.Value is null)
                {
                    Orders.Clear();
                    _totalCount = 0;
                    RebuildPageButtons();
                    ErrorMessage = result.Error ?? "Failed to load orders.";
                    OnPropertyChanged(nameof(ResultText));
                    return;
                }

                Orders.Clear();
                foreach (var order in ApplySorting(result.Value.Items))
                {
                    Orders.Add(order);
                }

                _totalCount = result.Value.TotalCount;
                RebuildPageButtons();
                if (SelectedOrder is not null)
                {
                    SelectedOrder = Orders.FirstOrDefault(order => order.OrderId == SelectedOrder.OrderId) ?? Orders.FirstOrDefault();
                }
                else
                {
                    SelectedOrder = Orders.FirstOrDefault();
                }

                OnPropertyChanged(nameof(ResultText));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private IEnumerable<OrderDto> ApplySorting(IEnumerable<OrderDto> items)
        {
            var descending = string.Equals(SelectedSortDirection?.Key, "desc", StringComparison.OrdinalIgnoreCase);
            return (SelectedSortField?.Key ?? "createdAt") switch
            {
                "customer" => descending
                    ? items.OrderByDescending(item => item.Customer?.Name)
                    : items.OrderBy(item => item.Customer?.Name),
                "amount" => descending
                    ? items.OrderByDescending(item => item.FinalAmount)
                    : items.OrderBy(item => item.FinalAmount),
                _ => descending
                    ? items.OrderByDescending(item => ParseDateTime(item.CreatedAt))
                    : items.OrderBy(item => ParseDateTime(item.CreatedAt)),
            };
        }

        private async Task PreviousPageAsync()
        {
            if (CurrentPage <= 1)
            {
                return;
            }

            CurrentPage--;
            await LoadOrdersAsync();
        }

        private async Task NextPageAsync()
        {
            if (CurrentPage >= TotalPages)
            {
                return;
            }

            CurrentPage++;
            await LoadOrdersAsync();
        }

        private async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPages || page == CurrentPage)
            {
                return;
            }

            CurrentPage = page;
            await LoadOrdersAsync();
        }

        private async Task BeginCreateOrderAsync()
        {
            ResetEditor();
            IsEditMode = false;
            IsEditorOpen = true;
            StartAutoSave();
            await LoadAvailableInstancesAsync();

            await TryRestoreDraftAsync();

            UpdateSuggestionsFromCache();
            NotifyEditorTotalsChanged();
        }

        private async Task BeginEditOrderAsync()
        {
            if (SelectedOrder is null || !CanModifySelectedCreatedOrder)
            {
                return;
            }

            ResetEditor();
            StopAutoSave();
            IsEditMode = true;
            IsEditorOpen = true;
            EditorCustomerId = SelectedOrder.Customer?.CustomerId ?? string.Empty;
            EditorPromotionCode = SelectedOrder.Promotion?.Code ?? string.Empty;
            EditorNotes = SelectedOrder.Notes ?? string.Empty;

            if (SelectedOrder.Promotion is not null)
            {
                _appliedPromotion = new PromotionValidationDto
                {
                    IsValid = true,
                    Message = "Promotion loaded from order.",
                    Promotion = SelectedOrder.Promotion
                };
            }

            await LoadAvailableInstancesAsync();

            foreach (var group in SelectedOrder.OrderItems.GroupBy(item => item.Instance.Product?.ProductId ?? 0))
            {
                var firstInstance = group.First().Instance;
                var cartItem = CreateCartItem(firstInstance);
                foreach (var extraInstance in group.Skip(1).Select(item => item.Instance))
                {
                    cartItem.AddInstance(extraInstance);
                }

                CartItems.Add(cartItem);
            }

            UpdateSuggestionsFromCache();
            NotifyEditorTotalsChanged();
            _isDraftDirty = false;
        }

        private async Task LoadAvailableInstancesAsync()
        {
            if (!IsEditorOpen)
            {
                return;
            }

            IsEditorBusy = true;
            EditorErrorMessage = string.Empty;
            EditorStatusMessage = "Loading available products...";

            try
            {
                var result = await _orderService.GetAvailableInstancesAsync(new ProductInstanceFilterDto
                {
                    Page = 1,
                    PageSize = 400
                });

                if (!result.IsSuccess || result.Value is null)
                {
                    _availableInstancesCache.Clear();
                    ProductSuggestions.Clear();
                    EditorErrorMessage = result.Error ?? "Failed to load available products.";
                    return;
                }

                _availableInstancesCache.Clear();
                _availableInstancesCache.AddRange(result.Value.Items);
                UpdateSuggestionsFromCache();
            }
            finally
            {
                EditorStatusMessage = string.Empty;
                IsEditorBusy = false;
            }
        }

        public void ChooseSuggestedProduct(OrderProductSuggestionViewModel? suggestion)
        {
            if (suggestion is null)
            {
                return;
            }

            AddInstanceToCart(suggestion.ProductId);
            InstanceSearchQuery = string.Empty;
        }

        private void AddInstanceToCart(int productId)
        {
            var selectedInstanceIds = CartItems
                .SelectMany(item => item.InstanceIds)
                .ToHashSet();

            var nextInstance = _availableInstancesCache
                .Where(item => item.Product?.ProductId == productId)
                .OrderBy(item => item.SerialNumber)
                .FirstOrDefault(item => !selectedInstanceIds.Contains(item.InstanceId));

            if (nextInstance is null)
            {
                EditorErrorMessage = "No more available serials for this product.";
                UpdateSuggestionsFromCache();
                return;
            }

            var existingItem = CartItems.FirstOrDefault(item => item.ProductId == productId);
            if (existingItem is null)
            {
                CartItems.Add(CreateCartItem(nextInstance));
            }
            else
            {
                existingItem.AddInstance(nextInstance);
            }

            MarkDraftDirty();
            UpdateSuggestionsFromCache();
            NotifyEditorTotalsChanged();
        }

        private OrderCartItemViewModel CreateCartItem(ProductInstanceDto instance)
        {
            var product = instance.Product;
            return new OrderCartItemViewModel(
                product?.ProductId ?? 0,
                product?.Name ?? "Unknown product",
                product?.Sku ?? string.Empty,
                product?.SellingPrice ?? 0,
                NormalizeImageUri(product?.Images.OrderBy(image => image.DisplayOrder).FirstOrDefault()?.ImageUrl),
                BuildHighlightLines(product),
                instance);
        }

        private static IReadOnlyList<string> BuildHighlightLines(ProductDto? product)
        {
            if (product is null)
            {
                return [];
            }

            var lines = new List<string>();
            if (product.Specifications is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in new[] { "cpu", "processor", "gpu", "graphics", "ram", "memory", "display", "screen" })
                {
                    if (!TryGetPropertyIgnoreCase(jsonElement, key, out var property))
                    {
                        continue;
                    }

                    var value = property.ToString();
                    if (!string.IsNullOrWhiteSpace(value) && !lines.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        lines.Add(value);
                    }
                }
            }

            if (lines.Count == 0)
            {
                if (product.Brand is not null)
                {
                    lines.Add(product.Brand.Name);
                }

                if (product.Categories.Count > 0)
                {
                    lines.Add(string.Join(" / ", product.Categories.Select(category => category.Name)));
                }

                lines.Add($"{product.WarrantyMonths} month warranty");
            }

            return [.. lines.Take(4)];
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement property)
        {
            foreach (var candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }

            property = default;
            return false;
        }

        private Uri? NormalizeImageUri(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri;
            }

            if (!Uri.TryCreate(_configService.GetServerUrl(), UriKind.Absolute, out var graphQlUri))
            {
                return null;
            }

            var baseOrigin = new Uri(graphQlUri.GetLeftPart(UriPartial.Authority));
            var normalizedPath = imageUrl.StartsWith("/", StringComparison.Ordinal) ? imageUrl : $"/{imageUrl}";

            return Uri.TryCreate(baseOrigin, normalizedPath, out var resolvedUri)
                ? resolvedUri
                : null;
        }

        private void UpdateSuggestionsFromCache()
        {
            var selectedInstanceIds = CartItems
                .SelectMany(item => item.InstanceIds)
                .ToHashSet();

            var search = InstanceSearchQuery?.Trim();
            var groups = _availableInstancesCache
                .Where(instance => instance.Product is not null)
                .GroupBy(instance => instance.Product!.ProductId)
                .Select(group =>
                {
                    var product = group.First().Product!;
                    var matchesSearch = string.IsNullOrWhiteSpace(search)
                        || product.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || product.Sku.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || group.Any(instance => instance.SerialNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
                    var availableCount = group.Count(instance => !selectedInstanceIds.Contains(instance.InstanceId));
                    return new
                    {
                        Product = product,
                        AvailableCount = availableCount,
                        MatchesSearch = matchesSearch
                    };
                })
                .Where(item => item.AvailableCount > 0 && item.MatchesSearch)
                .OrderBy(item => item.Product.Name)
                .ThenBy(item => item.Product.Sku)
                .Select(item => new OrderProductSuggestionViewModel(
                    item.Product.ProductId,
                    item.Product.Name,
                    item.Product.Sku,
                    item.Product.SellingPrice,
                    item.AvailableCount))
                .ToList();

            ProductSuggestions.Clear();
            foreach (var suggestion in groups)
            {
                ProductSuggestions.Add(suggestion);
            }
        }

        private void IncreaseQuantity(OrderCartItemViewModel? item)
        {
            if (item is null)
            {
                return;
            }

            AddInstanceToCart(item.ProductId);
        }

        private void DecreaseQuantity(OrderCartItemViewModel? item)
        {
            if (item is null)
            {
                return;
            }

            if (item.Quantity <= 1)
            {
                CartItems.Remove(item);
            }
            else
            {
                item.RemoveLastInstance();
            }

            MarkDraftDirty();
            UpdateSuggestionsFromCache();
            NotifyEditorTotalsChanged();
        }

        private void RemoveCartItem(OrderCartItemViewModel? item)
        {
            if (item is null)
            {
                return;
            }

            CartItems.Remove(item);
            MarkDraftDirty();
            UpdateSuggestionsFromCache();
            NotifyEditorTotalsChanged();
        }

        private async Task CreateInlineCustomerAsync()
        {
            if (RequestCustomerEditorAsync is null)
            {
                return;
            }

            var input = await RequestCustomerEditorAsync(new CustomerEditorState());
            if (input is null)
            {
                return;
            }

            IsEditorBusy = true;
            try
            {
                var result = await _customerService.CreateAsync(new CreateCustomerInput
                {
                    Name = input.Name,
                    Phone = input.Phone,
                    Email = input.Email
                });

                if (!result.IsSuccess || result.Value is null)
                {
                    EditorErrorMessage = result.Error ?? "Failed to create customer.";
                    return;
                }

                await LoadCustomersAsync();
                EditorCustomerId = result.Value.CustomerId;
            }
            finally
            {
                IsEditorBusy = false;
            }
        }

        private async Task ApplyPromotionAsync()
        {
            EditorErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(EditorPromotionCode))
            {
                _appliedPromotion = null;
                NotifyEditorTotalsChanged();
                return;
            }

            IsEditorBusy = true;
            EditorStatusMessage = "Validating promotion...";
            try
            {
                var result = await _promotionService.ValidateAsync(EditorPromotionCode.Trim(), GetSelectedCustomerRank());
                if (!result.IsSuccess || result.Value is null || !result.Value.IsValid)
                {
                    _appliedPromotion = null;
                    EditorErrorMessage = result.Value?.Message ?? result.Error ?? "Promotion is invalid.";
                    NotifyEditorTotalsChanged();
                    return;
                }

                _appliedPromotion = result.Value;
                NotifyEditorTotalsChanged();
                MarkDraftDirty();
            }
            finally
            {
                EditorStatusMessage = string.Empty;
                IsEditorBusy = false;
            }
        }

        private async Task SaveOrderAsync()
        {
            EditorErrorMessage = string.Empty;

            if (CartItems.Count == 0)
            {
                EditorErrorMessage = "Select at least one product.";
                return;
            }

            IsEditorBusy = true;
            EditorStatusMessage = IsEditMode ? "Updating order..." : "Creating order...";

            try
            {
                var orderItems =
                    CartItems
                        .SelectMany(item => item.InstanceIds)
                        .Select(instanceId => new OrderItemInput
                        {
                            InstanceId = instanceId,
                            Quantity = 1
                        })
                        .ToList();

                if (IsEditMode && SelectedOrder is not null)
                {
                    var updateResult = await _orderService.UpdateAsync(SelectedOrder.OrderId, new UpdateOrderInput
                    {
                        CustomerId = string.IsNullOrWhiteSpace(EditorCustomerId) ? null : EditorCustomerId,
                        PromotionCode = string.IsNullOrWhiteSpace(EditorPromotionCode) ? null : EditorPromotionCode.Trim(),
                        Notes = string.IsNullOrWhiteSpace(EditorNotes) ? null : EditorNotes.Trim(),
                        Items = orderItems
                    });

                    if (!updateResult.IsSuccess)
                    {
                        EditorErrorMessage = updateResult.Error ?? "Failed to update order.";
                        return;
                    }
                }
                else
                {
                    var createResult = await _orderService.CreateAsync(new CreateOrderInput
                    {
                        CustomerId = string.IsNullOrWhiteSpace(EditorCustomerId) ? null : EditorCustomerId,
                        PromotionCode = string.IsNullOrWhiteSpace(EditorPromotionCode) ? null : EditorPromotionCode.Trim(),
                        Notes = string.IsNullOrWhiteSpace(EditorNotes) ? null : EditorNotes.Trim(),
                        Items = orderItems
                    });

                    if (!createResult.IsSuccess)
                    {
                        EditorErrorMessage = createResult.Error ?? "Failed to create order.";
                        return;
                    }
                }

                await ClearDraftAsync(resetRestoredState: true);
                CancelEditor();
                await LoadOrdersAsync();
            }
            finally
            {
                EditorStatusMessage = string.Empty;
                IsEditorBusy = false;
            }
        }

        private async Task MarkPaidAsync()
        {
            if (SelectedOrder is null)
            {
                return;
            }

            var result = await _orderService.UpdateStatusAsync(SelectedOrder.OrderId, "Paid");
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to mark order as paid.";
                return;
            }

            await LoadOrdersAsync();
        }

        private async Task CancelOrderAsync()
        {
            if (SelectedOrder is null)
            {
                return;
            }

            var result = await _orderService.UpdateStatusAsync(SelectedOrder.OrderId, "Cancelled");
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to cancel order.";
                return;
            }

            await LoadOrdersAsync();
        }

        private async Task DeleteOrderAsync()
        {
            if (SelectedOrder is null)
            {
                return;
            }

            if (ConfirmDeleteOrderAsync is not null)
            {
                var confirmed = await ConfirmDeleteOrderAsync(SelectedOrder);
                if (!confirmed)
                {
                    return;
                }
            }

            var result = await _orderService.DeleteAsync(SelectedOrder.OrderId);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to delete order.";
                return;
            }

            await LoadOrdersAsync();
        }

        private async Task PrintInvoiceAsync()
        {
            if (SelectedOrder is null || RequestInvoicePathAsync is null)
            {
                return;
            }

            var latestOrderResult = await _orderService.GetByIdAsync(SelectedOrder.OrderId);
            if (!latestOrderResult.IsSuccess || latestOrderResult.Value is null)
            {
                ErrorMessage = latestOrderResult.Error ?? "Failed to load invoice data.";
                return;
            }

            var fileName = $"invoice-{latestOrderResult.Value.OrderId[..Math.Min(8, latestOrderResult.Value.OrderId.Length)]}.pdf";
            var outputPath = await RequestInvoicePathAsync(fileName);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }

            var exportResult = await _invoiceService.GenerateInvoicePdfAsync(latestOrderResult.Value, outputPath);
            if (!exportResult.IsSuccess)
            {
                ErrorMessage = exportResult.Error ?? "Failed to export invoice.";
                return;
            }

            ErrorMessage = string.Empty;
        }

        private void CancelEditor()
        {
            StopAutoSave();
            IsEditorOpen = false;
            IsEditMode = false;
            ResetEditor();
        }

        private void ResetEditor()
        {
            EditorCustomerId = string.Empty;
            EditorPromotionCode = string.Empty;
            EditorNotes = string.Empty;
            EditorErrorMessage = string.Empty;
            EditorStatusMessage = string.Empty;
            InstanceSearchQuery = string.Empty;
            _appliedPromotion = null;
            _availableInstancesCache.Clear();
            ProductSuggestions.Clear();
            CartItems.Clear();
            HasSavedDraft = false;
            IsDraftRestored = false;
            DraftEventMessage = string.Empty;
            _isDraftDirty = false;
            NotifyEditorTotalsChanged();
        }

        private void NotifyEditorTotalsChanged()
        {
            OnPropertyChanged(nameof(EditorSubtotal));
            OnPropertyChanged(nameof(EditorDiscountAmount));
            OnPropertyChanged(nameof(EditorFinalAmount));
            OnPropertyChanged(nameof(PromotionSummary));
            OnPropertyChanged(nameof(HasSelectedOrderItems));
            OnPropertyChanged(nameof(SelectedItemCount));
            OnPropertyChanged(nameof(CartSummaryText));
        }

        private string? GetSelectedCustomerRank()
        {
            if (string.IsNullOrWhiteSpace(EditorCustomerId))
            {
                return null;
            }

            return Customers.FirstOrDefault(customer => customer.CustomerId == EditorCustomerId)?.Rank;
        }

        public static string FormatCurrency(double value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture) + " VND";
        }

        public static string FormatDate(string value)
        {
            return DateTime.TryParse(value, out var parsed)
                ? parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : value;
        }

        private static DateTime ParseDateTime(string? value)
        {
            return DateTime.TryParse(value, out var parsed)
                ? parsed
                : DateTime.MinValue;
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
                await Task.Delay(350, token);
                if (!token.IsCancellationRequested)
                {
                    await LoadOrdersAsync();
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void DebounceInstanceSearch()
        {
            _instanceDebounceCts?.Cancel();
            _instanceDebounceCts?.Dispose();
            _instanceDebounceCts = new CancellationTokenSource();
            _ = DebounceInstanceSearchAsync(_instanceDebounceCts.Token);
        }

        private async Task DebounceInstanceSearchAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(250, token);
                if (!token.IsCancellationRequested)
                {
                    UpdateSuggestionsFromCache();
                }
            }
            catch (TaskCanceledException)
            {
            }

            await Task.CompletedTask;
        }

        private void StartAutoSave()
        {
            if (_autoSaveTimer.IsEnabled)
            {
                return;
            }

            _autoSaveTimer.Start();
            IsAutoSaveActive = true;
        }

        private void StopAutoSave()
        {
            if (_autoSaveTimer.IsEnabled)
            {
                _autoSaveTimer.Stop();
            }

            IsAutoSaveActive = false;
        }

        private void MarkDraftDirty()
        {
            if (_isRestoringDraft || IsEditMode || !IsEditorOpen)
            {
                return;
            }

            _isDraftDirty = true;
            DraftEventMessage = string.Empty;
        }

        private async Task<bool> TryRestoreDraftAsync()
        {
            if (IsEditMode)
            {
                return false;
            }

            var localSettings = ApplicationData.Current.LocalSettings;
            if (!localSettings.Values.TryGetValue(DraftStorageKey, out var rawDraft)
                || rawDraft is not string draftJson
                || string.IsNullOrWhiteSpace(draftJson))
            {
                return false;
            }

            CreateOrderDraft? draft;
            try
            {
                draft = JsonSerializer.Deserialize<CreateOrderDraft>(draftJson);
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
            DraftEventMessage = "Draft restored automatically.";
            _isDraftDirty = false;
            return true;
        }

        private async Task RestoreDraftAsync(CreateOrderDraft draft)
        {
            _isRestoringDraft = true;
            try
            {
                EditorCustomerId = draft.CustomerId ?? string.Empty;
                EditorPromotionCode = draft.PromotionCode ?? string.Empty;
                EditorNotes = draft.Notes ?? string.Empty;

                foreach (var group in (draft.SelectedInstanceIds ?? []).Distinct().Select(id => _availableInstancesCache.FirstOrDefault(item => item.InstanceId == id)).Where(item => item is not null).Cast<ProductInstanceDto>().GroupBy(item => item.Product?.ProductId ?? 0))
                {
                    var first = group.First();
                    var cartItem = CreateCartItem(first);
                    foreach (var extra in group.Skip(1))
                    {
                        cartItem.AddInstance(extra);
                    }

                    CartItems.Add(cartItem);
                }

                if (!string.IsNullOrWhiteSpace(EditorPromotionCode))
                {
                    await ApplyPromotionAsync();
                }
            }
            finally
            {
                _isRestoringDraft = false;
            }
        }

        private async Task SaveDraftIfDirtyAsync()
        {
            if (!_isDraftDirty || !IsEditorOpen || IsEditMode || _isRestoringDraft)
            {
                return;
            }

            var draft = BuildDraft();
            var serializedDraft = JsonSerializer.Serialize(draft);
            ApplicationData.Current.LocalSettings.Values[DraftStorageKey] = serializedDraft;
            HasSavedDraft = true;
            if (!IsDraftRestored)
            {
                DraftEventMessage = string.Empty;
            }

            _isDraftDirty = false;
            await Task.CompletedTask;
        }

        private CreateOrderDraft BuildDraft()
        {
            return new CreateOrderDraft
            {
                CustomerId = EditorCustomerId,
                PromotionCode = EditorPromotionCode,
                Notes = EditorNotes,
                SelectedInstanceIds = [.. CartItems.SelectMany(item => item.InstanceIds)]
            };
        }

        private async Task DiscardDraftAsync()
        {
            await ClearDraftAsync(resetRestoredState: true);
            if (IsEditorOpen && !IsEditMode)
            {
                _isRestoringDraft = true;
                EditorCustomerId = string.Empty;
                EditorPromotionCode = string.Empty;
                EditorNotes = string.Empty;
                EditorErrorMessage = string.Empty;
                EditorStatusMessage = string.Empty;
                InstanceSearchQuery = string.Empty;
                _appliedPromotion = null;
                CartItems.Clear();
                _isRestoringDraft = false;
                UpdateSuggestionsFromCache();
                NotifyEditorTotalsChanged();
                _isDraftDirty = false;
            }

            DraftEventMessage = "Draft discarded.";
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

        private void RebuildPageButtons()
        {
            PageButtons.Clear();

            PageButtons.Add(new PageButtonItem
            {
                Label = "<- Previous",
                PageNumber = CurrentPage - 1,
                IsEnabled = CurrentPage > 1,
                IsCurrent = false
            });

            int? previousPage = null;
            foreach (var page in BuildPageNumbers(CurrentPage, TotalPages))
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
                    IsEnabled = page != CurrentPage,
                    IsCurrent = page == CurrentPage
                });
                previousPage = page;
            }

            PageButtons.Add(new PageButtonItem
            {
                Label = "Next ->",
                PageNumber = CurrentPage + 1,
                IsEnabled = CurrentPage < TotalPages,
                IsCurrent = false
            });
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
                _ = LoadOrdersAsync();
            }
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

        private static int NormalizePageSize(int value)
        {
            return value is 5 or 10 or 15 or 20 ? value : DefaultPageSize;
        }
    }

    public class OrderProductSuggestionViewModel
    {
        public OrderProductSuggestionViewModel(
            int productId,
            string productName,
            string productSku,
            double unitPrice,
            int availableCount)
        {
            ProductId = productId;
            ProductName = productName;
            ProductSku = productSku;
            UnitPrice = unitPrice;
            AvailableCount = availableCount;
        }

        public int ProductId { get; }
        public string ProductName { get; }
        public string ProductSku { get; }
        public double UnitPrice { get; }
        public int AvailableCount { get; }
        public string DisplayText => $"{ProductName} ({ProductSku})";
        public string SecondaryText => $"{OrdersViewModel.FormatCurrency(UnitPrice)} | {AvailableCount} serial(s) available";
    }

    public class OrderCartItemViewModel : ObservableObject
    {
        private readonly List<ProductInstanceDto> _instances = [];

        public OrderCartItemViewModel(
            int productId,
            string productName,
            string productSku,
            double unitPrice,
            Uri? thumbnailUri,
            IReadOnlyList<string> highlightLines,
            ProductInstanceDto initialInstance)
        {
            ProductId = productId;
            ProductName = productName;
            ProductSku = productSku;
            UnitPrice = unitPrice;
            ThumbnailUri = thumbnailUri;
            HighlightLines = highlightLines;
            _instances.Add(initialInstance);
        }

        public int ProductId { get; }
        public string ProductName { get; }
        public string ProductSku { get; }
        public double UnitPrice { get; }
        public Uri? ThumbnailUri { get; }
        public bool HasThumbnail => ThumbnailUri is not null;
        public IReadOnlyList<string> HighlightLines { get; }
        public IReadOnlyList<int> InstanceIds => [.. _instances.Select(item => item.InstanceId)];
        public int Quantity => _instances.Count;
        public double LineTotal => UnitPrice * Quantity;
        public string UnitPriceDisplay => OrdersViewModel.FormatCurrency(UnitPrice);
        public string LineTotalDisplay => OrdersViewModel.FormatCurrency(LineTotal);
        public string PrimarySerial => _instances.FirstOrDefault()?.SerialNumber ?? string.Empty;
        public string SerialDisplay => Quantity <= 1 ? PrimarySerial : $"{PrimarySerial} +{Quantity - 1} more";

        public void AddInstance(ProductInstanceDto instance)
        {
            _instances.Add(instance);
            NotifyChanged();
        }

        public void RemoveLastInstance()
        {
            if (_instances.Count == 0)
            {
                return;
            }

            _instances.RemoveAt(_instances.Count - 1);
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            OnPropertyChanged(nameof(InstanceIds));
            OnPropertyChanged(nameof(Quantity));
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(LineTotalDisplay));
            OnPropertyChanged(nameof(PrimarySerial));
            OnPropertyChanged(nameof(SerialDisplay));
        }
    }

    public class OrderTableOption
    {
        public OrderTableOption(string key, string label)
        {
            Key = key;
            Label = label;
        }

        public string Key { get; }
        public string Label { get; }
    }

    public class CreateOrderDraft
    {
        public string? CustomerId { get; set; }
        public string? PromotionCode { get; set; }
        public string? Notes { get; set; }
        public List<int>? SelectedInstanceIds { get; set; }
    }
}
