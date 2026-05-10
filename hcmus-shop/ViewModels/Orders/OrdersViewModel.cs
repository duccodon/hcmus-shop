using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Customers.Dto;
using hcmus_shop.Services.Orders.Dto;
using hcmus_shop.ViewModels.Products;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Orders
{
    public class OrdersViewModel : ObservableObject
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IPromotionService _promotionService;
        private readonly IInvoiceService _invoiceService;
        private readonly IAuthService _authService;
        private CancellationTokenSource? _searchDebounceCts;

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

        public OrdersViewModel(
            IOrderService orderService,
            ICustomerService customerService,
            IPromotionService promotionService,
            IInvoiceService invoiceService,
            IAuthService authService)
        {
            _orderService = orderService;
            _customerService = customerService;
            _promotionService = promotionService;
            _invoiceService = invoiceService;
            _authService = authService;

            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized && !IsLoading);
            RefreshCommand = new AsyncRelayCommand(LoadOrdersAsync, () => !IsLoading);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => !IsLoading && CurrentPage > 1);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => !IsLoading && CurrentPage < TotalPages);
            GoToPageCommand = new AsyncRelayCommand<int>(GoToPageAsync);
            BeginCreateOrderCommand = new AsyncRelayCommand(BeginCreateOrderAsync, () => !IsLoading && !IsEditorBusy);
            BeginEditOrderCommand = new AsyncRelayCommand(BeginEditOrderAsync, () => CanModifySelectedCreatedOrder);
            SaveOrderCommand = new AsyncRelayCommand(SaveOrderAsync, () => IsEditorOpen && !IsEditorBusy);
            CancelEditorCommand = new RelayCommand(CancelEditor);
            SearchInstancesCommand = new AsyncRelayCommand(LoadAvailableInstancesAsync, () => IsEditorOpen && !IsEditorBusy);
            ApplyPromotionCommand = new AsyncRelayCommand(ApplyPromotionAsync, () => IsEditorOpen && !IsEditorBusy);
            MarkPaidCommand = new AsyncRelayCommand(MarkPaidAsync, () => CanModifySelectedCreatedOrder);
            CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync, () => CanModifySelectedCreatedOrder);
            DeleteOrderCommand = new AsyncRelayCommand(DeleteOrderAsync, () => CanDeleteSelectedOrder);
            PrintInvoiceCommand = new AsyncRelayCommand(PrintInvoiceAsync, () => SelectedOrder is not null && !IsLoading);

            SelectedStatusOptions.Add(string.Empty);
            SelectedStatusOptions.Add("Created");
            SelectedStatusOptions.Add("Paid");
            SelectedStatusOptions.Add("Cancelled");
        }

        public ObservableCollection<OrderDto> Orders { get; } = [];
        public ObservableCollection<CustomerDto> Customers { get; } = [];
        public ObservableCollection<SelectableInstanceViewModel> AvailableInstances { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [10, 20, 50];
        public ObservableCollection<PageButtonItem> PageButtons { get; } = [];
        public ObservableCollection<string> SelectedStatusOptions { get; } = [];

        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand<int> GoToPageCommand { get; }
        public IAsyncRelayCommand BeginCreateOrderCommand { get; }
        public IAsyncRelayCommand BeginEditOrderCommand { get; }
        public IAsyncRelayCommand SaveOrderCommand { get; }
        public IRelayCommand CancelEditorCommand { get; }
        public IAsyncRelayCommand SearchInstancesCommand { get; }
        public IAsyncRelayCommand ApplyPromotionCommand { get; }
        public IAsyncRelayCommand MarkPaidCommand { get; }
        public IAsyncRelayCommand CancelOrderCommand { get; }
        public IAsyncRelayCommand DeleteOrderCommand { get; }
        public IAsyncRelayCommand PrintInvoiceCommand { get; }

        public Func<string, Task<string?>>? RequestInvoicePathAsync { get; set; }
        public Func<OrderDto, Task<bool>>? ConfirmDeleteOrderAsync { get; set; }

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
                    SearchInstancesCommand.NotifyCanExecuteChanged();
                    ApplyPromotionCommand.NotifyCanExecuteChanged();
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
                if (SetProperty(ref _selectedPageSize, value))
                {
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
                    SearchInstancesCommand.NotifyCanExecuteChanged();
                    ApplyPromotionCommand.NotifyCanExecuteChanged();
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
                }
            }
        }

        public string EditorTitle => IsEditMode ? "Edit Order" : "Create Order";

        public string EditorCustomerId
        {
            get => _editorCustomerId;
            set => SetProperty(ref _editorCustomerId, value);
        }

        public string EditorPromotionCode
        {
            get => _editorPromotionCode;
            set => SetProperty(ref _editorPromotionCode, value);
        }

        public string EditorNotes
        {
            get => _editorNotes;
            set => SetProperty(ref _editorNotes, value);
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
            set => SetProperty(ref _instanceSearchQuery, value);
        }

        public bool IsEmpty => !IsLoading && Orders.Count == 0 && !HasError;

        public string ResultText =>
            _totalCount == 0
                ? "0 orders"
                : $"{((_currentPage - 1) * SelectedPageSize) + 1}-{Math.Min(_currentPage * SelectedPageSize, _totalCount)} of {_totalCount}";

        public string SelectedOrderSummary =>
            SelectedOrder is null
                ? "Select an order to view details."
                : $"{SelectedOrder.Status} | {FormatCurrency(SelectedOrder.FinalAmount)} | {FormatDate(SelectedOrder.CreatedAt)}";

        public string PromotionSummary =>
            _appliedPromotion?.IsValid == true && _appliedPromotion.Promotion is not null
                ? $"Applied: {_appliedPromotion.Promotion.Code}"
                : "No promotion applied";

        public double EditorSubtotal =>
            AvailableInstances.Where(instance => instance.IsSelected).Sum(instance => instance.UnitPrice);

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

        public bool CanModifySelectedCreatedOrder =>
            SelectedOrder is not null &&
            string.Equals(SelectedOrder.Status, "Created", StringComparison.OrdinalIgnoreCase) &&
            !IsLoading;

        public bool CanDeleteSelectedOrder => CanModifySelectedCreatedOrder && _authService.HasRole("Admin");

        private int TotalPages => Math.Max(1, (int)Math.Ceiling(Math.Max(_totalCount, 1) / (double)SelectedPageSize));

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
            foreach (var customer in result.Value.Items.OrderBy(customer => customer.Name))
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
                foreach (var order in result.Value.Items)
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
            EditorCustomerId = Customers.FirstOrDefault()?.CustomerId ?? string.Empty;
            await LoadAvailableInstancesAsync();
        }

        private async Task BeginEditOrderAsync()
        {
            if (SelectedOrder is null || !CanModifySelectedCreatedOrder)
            {
                return;
            }

            ResetEditor();
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

            var selectedIds = SelectedOrder.OrderItems.Select(item => item.Instance.InstanceId).ToHashSet();
            foreach (var instance in AvailableInstances)
            {
                instance.IsSelected = selectedIds.Contains(instance.InstanceId);
            }
            NotifyEditorTotalsChanged();
        }

        private async Task LoadAvailableInstancesAsync()
        {
            if (!IsEditorOpen)
            {
                return;
            }

            IsEditorBusy = true;
            EditorErrorMessage = string.Empty;
            EditorStatusMessage = "Loading available serials...";

            try
            {
                var result = await _orderService.GetAvailableInstancesAsync(new ProductInstanceFilterDto
                {
                    Search = string.IsNullOrWhiteSpace(InstanceSearchQuery) ? null : InstanceSearchQuery.Trim(),
                    Page = 1,
                    PageSize = 400
                });

                if (!result.IsSuccess || result.Value is null)
                {
                    AvailableInstances.Clear();
                    EditorErrorMessage = result.Error ?? "Failed to load available serials.";
                    return;
                }

                var selectedIds = AvailableInstances.Where(instance => instance.IsSelected).Select(instance => instance.InstanceId).ToHashSet();

                AvailableInstances.Clear();
                foreach (var instance in result.Value.Items.OrderBy(instance => instance.Product?.Name).ThenBy(instance => instance.SerialNumber))
                {
                    var selectable = new SelectableInstanceViewModel(instance);
                    selectable.IsSelected = selectedIds.Contains(selectable.InstanceId);
                    selectable.PropertyChanged += SelectableInstance_PropertyChanged;
                    AvailableInstances.Add(selectable);
                }

                NotifyEditorTotalsChanged();
            }
            finally
            {
                EditorStatusMessage = string.Empty;
                IsEditorBusy = false;
            }
        }

        private void SelectableInstance_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableInstanceViewModel.IsSelected))
            {
                NotifyEditorTotalsChanged();
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

            if (string.IsNullOrWhiteSpace(EditorCustomerId))
            {
                EditorErrorMessage = "Customer is required.";
                return;
            }

            var selectedItems = AvailableInstances.Where(instance => instance.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                EditorErrorMessage = "Select at least one product serial.";
                return;
            }

            IsEditorBusy = true;
            EditorStatusMessage = IsEditMode ? "Updating order..." : "Creating order...";

            try
            {
                if (IsEditMode && SelectedOrder is not null)
                {
                    var updateResult = await _orderService.UpdateAsync(SelectedOrder.OrderId, new UpdateOrderInput
                    {
                        CustomerId = EditorCustomerId,
                        PromotionCode = string.IsNullOrWhiteSpace(EditorPromotionCode) ? null : EditorPromotionCode.Trim(),
                        Notes = string.IsNullOrWhiteSpace(EditorNotes) ? null : EditorNotes.Trim(),
                        Items =
                        [
                            .. selectedItems.Select(item => new OrderItemInput
                            {
                                InstanceId = item.InstanceId,
                                Quantity = 1
                            })
                        ]
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
                        CustomerId = EditorCustomerId,
                        PromotionCode = string.IsNullOrWhiteSpace(EditorPromotionCode) ? null : EditorPromotionCode.Trim(),
                        Notes = string.IsNullOrWhiteSpace(EditorNotes) ? null : EditorNotes.Trim(),
                        Items =
                        [
                            .. selectedItems.Select(item => new OrderItemInput
                            {
                                InstanceId = item.InstanceId,
                                Quantity = 1
                            })
                        ]
                    });

                    if (!createResult.IsSuccess)
                    {
                        EditorErrorMessage = createResult.Error ?? "Failed to create order.";
                        return;
                    }
                }

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
            foreach (var instance in AvailableInstances)
            {
                instance.PropertyChanged -= SelectableInstance_PropertyChanged;
            }
            AvailableInstances.Clear();
            NotifyEditorTotalsChanged();
        }

        private void NotifyEditorTotalsChanged()
        {
            OnPropertyChanged(nameof(EditorSubtotal));
            OnPropertyChanged(nameof(EditorDiscountAmount));
            OnPropertyChanged(nameof(EditorFinalAmount));
            OnPropertyChanged(nameof(PromotionSummary));
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

    public partial class SelectableInstanceViewModel : ObservableObject
    {
        public SelectableInstanceViewModel(ProductInstanceDto instance)
        {
            Instance = instance;
        }

        public ProductInstanceDto Instance { get; }

        public int InstanceId => Instance.InstanceId;
        public string ProductName => Instance.Product?.Name ?? "Unknown product";
        public string ProductSku => Instance.Product?.Sku ?? string.Empty;
        public string SerialNumber => Instance.SerialNumber;
        public double UnitPrice => Instance.Product?.SellingPrice ?? 0;

        [ObservableProperty]
        private bool _isSelected;
    }
}
