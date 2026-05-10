using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Customers.Dto;
using hcmus_shop.ViewModels.Products;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Customers
{
    public class CustomersViewModel : ObservableObject
    {
        private readonly ICustomerService _customerService;
        private readonly IAuthService _authService;
        private CancellationTokenSource? _searchDebounceCts;
        private bool _isInitialized;
        private bool _isLoading;
        private string _searchQuery = string.Empty;
        private string _errorMessage = string.Empty;
        private int _selectedPageSize = 10;
        private int _currentPage = 1;
        private int _totalCount;

        public CustomersViewModel(ICustomerService customerService, IAuthService authService)
        {
            _customerService = customerService;
            _authService = authService;

            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized && !IsLoading);
            RefreshCommand = new AsyncRelayCommand(LoadCustomersAsync, () => !IsLoading);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => !IsLoading && CurrentPage > 1);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => !IsLoading && CurrentPage < TotalPages);
            GoToPageCommand = new AsyncRelayCommand<int>(GoToPageAsync);
            AddCustomerCommand = new AsyncRelayCommand(AddCustomerAsync, () => !IsLoading);
            OpenCustomerDetailCommand = new RelayCommand<CustomerDto?>(OpenCustomerDetail, customer => CanViewCustomerDetails && customer is not null);
            DeleteCustomerCommand = new AsyncRelayCommand<CustomerDto>(DeleteCustomerAsync, customer => CanDeleteCustomers && customer is not null);
        }

        public ObservableCollection<CustomerDto> Customers { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [10, 20, 50];
        public ObservableCollection<PageButtonItem> PageButtons { get; } = [];

        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand<int> GoToPageCommand { get; }
        public IAsyncRelayCommand AddCustomerCommand { get; }
        public IRelayCommand<CustomerDto?> OpenCustomerDetailCommand { get; }
        public IAsyncRelayCommand<CustomerDto> DeleteCustomerCommand { get; }

        public Func<CustomerEditorState, Task<CustomerEditorResult?>>? RequestCustomerEditorAsync { get; set; }
        public Func<CustomerDto, Task<bool>>? ConfirmDeleteCustomerAsync { get; set; }
        public Action<string>? NavigateToCustomerRequested { get; set; }

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
                    RefreshCommand.NotifyCanExecuteChanged();
                    PreviousPageCommand.NotifyCanExecuteChanged();
                    NextPageCommand.NotifyCanExecuteChanged();
                    AddCustomerCommand.NotifyCanExecuteChanged();
                    OpenCustomerDetailCommand.NotifyCanExecuteChanged();
                    DeleteCustomerCommand.NotifyCanExecuteChanged();
                }
            }
        }

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
        public bool IsAdmin => _authService.HasRole("Admin");
        public bool CanViewCustomerDetails => IsAdmin;
        public bool CanDeleteCustomers => IsAdmin;
        public string AccessSummary =>
            IsAdmin
                ? "Admin can open a full customer profile, update contact details, and delete a customer."
                : "Sales can create customers and use them in orders. Full customer profile is admin-only.";

        public int SelectedPageSize
        {
            get => _selectedPageSize;
            set
            {
                if (SetProperty(ref _selectedPageSize, value))
                {
                    _currentPage = 1;
                    _ = LoadCustomersAsync();
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

        public string ResultText =>
            _totalCount == 0
                ? "0 customers"
                : $"{((_currentPage - 1) * SelectedPageSize) + 1}-{Math.Min(_currentPage * SelectedPageSize, _totalCount)} of {_totalCount}";

        public bool IsEmpty => !IsLoading && Customers.Count == 0 && !HasError;

        private int TotalPages => Math.Max(1, (int)Math.Ceiling(Math.Max(_totalCount, 1) / (double)SelectedPageSize));

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            await LoadCustomersAsync();
            IsInitialized = true;
        }

        private async Task LoadCustomersAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _customerService.GetAllAsync(new CustomerFilterDto
                {
                    Search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                    Page = CurrentPage,
                    PageSize = SelectedPageSize
                });

                if (!result.IsSuccess || result.Value is null)
                {
                    Customers.Clear();
                    _totalCount = 0;
                    RebuildPageButtons();
                    ErrorMessage = result.Error ?? "Failed to load customers.";
                    OnPropertyChanged(nameof(ResultText));
                    OnPropertyChanged(nameof(IsEmpty));
                    return;
                }

                Customers.Clear();
                foreach (var customer in result.Value.Items)
                {
                    Customers.Add(customer);
                }

                _totalCount = result.Value.TotalCount;
                RebuildPageButtons();
                OnPropertyChanged(nameof(ResultText));
                OnPropertyChanged(nameof(IsEmpty));
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
            await LoadCustomersAsync();
        }

        private async Task NextPageAsync()
        {
            if (CurrentPage >= TotalPages)
            {
                return;
            }

            CurrentPage++;
            await LoadCustomersAsync();
        }

        private async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPages || page == CurrentPage)
            {
                return;
            }

            CurrentPage = page;
            await LoadCustomersAsync();
        }

        private async Task AddCustomerAsync()
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

            var result = await _customerService.CreateAsync(new CreateCustomerInput
            {
                Name = input.Name,
                Phone = input.Phone,
                Email = input.Email
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to create customer.";
                return;
            }

            await LoadCustomersAsync();
        }

        private void OpenCustomerDetail(CustomerDto? customer)
        {
            if (!CanViewCustomerDetails || customer is null)
            {
                return;
            }

            NavigateToCustomerRequested?.Invoke(customer.CustomerId);
        }

        private async Task DeleteCustomerAsync(CustomerDto? customer)
        {
            if (!CanDeleteCustomers || customer is null)
            {
                return;
            }

            if (ConfirmDeleteCustomerAsync is not null)
            {
                var confirmed = await ConfirmDeleteCustomerAsync(customer);
                if (!confirmed)
                {
                    return;
                }
            }

            var result = await _customerService.DeleteAsync(customer.CustomerId);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to delete customer.";
                return;
            }

            if (Customers.Count == 1 && CurrentPage > 1)
            {
                CurrentPage--;
            }

            await LoadCustomersAsync();
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
                    await LoadCustomersAsync();
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

    public class CustomerEditorState
    {
        public bool IsEditMode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class CustomerEditorResult
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
