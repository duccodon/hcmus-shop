using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Users.Dto;
using hcmus_shop.ViewModels.Products;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Admin
{
    public class SalesUsersViewModel : ObservableObject
    {
        private const int DefaultPageSize = 10;
        private readonly IUserService _userService;
        private readonly ISettingsService _settingsService;
        private CancellationTokenSource? _searchDebounceCts;
        private bool _isInitialized;
        private bool _isLoading;
        private string _searchQuery = string.Empty;
        private string _errorMessage = string.Empty;
        private int _currentPage = 1;
        private int _selectedPageSize = DefaultPageSize;

        public SalesUsersViewModel(IUserService userService, ISettingsService settingsService)
        {
            _userService = userService;
            _settingsService = settingsService;
            _selectedPageSize = NormalizePageSize(_settingsService.PageSize);
            if (_settingsService.PageSize != _selectedPageSize)
            {
                _settingsService.PageSize = _selectedPageSize;
            }

            _settingsService.SettingsChanged += OnSettingsChanged;

            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized && !IsLoading);
            RefreshCommand = new AsyncRelayCommand(LoadUsersAsync, () => !IsLoading);
            GoToPageCommand = new AsyncRelayCommand<int>(GoToPageAsync);
            AddUserCommand = new AsyncRelayCommand(AddUserAsync, () => !IsLoading);
            EditUserCommand = new AsyncRelayCommand<UserDto>(EditUserAsync, user => !IsLoading && user is not null);
            DeleteUserCommand = new AsyncRelayCommand<UserDto>(DeleteUserAsync, user => !IsLoading && user is not null);
        }

        public ObservableCollection<UserDto> Users { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [5, 10, 15, 20];
        public ObservableCollection<PageButtonItem> PageButtons { get; } = [];

        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand<int> GoToPageCommand { get; }
        public IAsyncRelayCommand AddUserCommand { get; }
        public IAsyncRelayCommand<UserDto> EditUserCommand { get; }
        public IAsyncRelayCommand<UserDto> DeleteUserCommand { get; }

        public Func<UserEditorState, Task<UserEditorResult?>>? RequestUserEditorAsync { get; set; }
        public Func<UserDto, Task<bool>>? ConfirmDeleteUserAsync { get; set; }

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
                    AddUserCommand.NotifyCanExecuteChanged();
                    EditUserCommand.NotifyCanExecuteChanged();
                    DeleteUserCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IsEmpty));
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
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool IsEmpty => !IsLoading && !HasError && Users.Count == 0;

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
                    _ = LoadUsersAsync();
                }
            }
        }

        public string ResultText =>
            _totalCount == 0
                ? "Result 0 of 0"
                : $"Result {((_currentPage - 1) * SelectedPageSize) + 1}-{Math.Min(_currentPage * SelectedPageSize, _totalCount)} of {_totalCount}";

        private int _totalCount;
        private int TotalPages => Math.Max(1, (int)Math.Ceiling(Math.Max(_totalCount, 1) / (double)SelectedPageSize));

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            await LoadUsersAsync();
            IsInitialized = true;
        }

        private async Task LoadUsersAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _userService.GetAllAsync(new UserFilterDto
                {
                    Search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                    Role = "Sale",
                    Page = _currentPage,
                    PageSize = SelectedPageSize
                });

                if (!result.IsSuccess || result.Value is null)
                {
                    Users.Clear();
                    _totalCount = 0;
                    RebuildPageButtons();
                    ErrorMessage = result.Error ?? "Failed to load sales users.";
                    OnPropertyChanged(nameof(ResultText));
                    return;
                }

                _totalCount = result.Value.TotalCount;
                Users.Clear();
                foreach (var user in result.Value.Items.OrderByDescending(item => ParseDate(item.CreatedAt)))
                {
                    Users.Add(user);
                }

                RebuildPageButtons();
                OnPropertyChanged(nameof(ResultText));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPages || page == _currentPage)
            {
                return;
            }

            _currentPage = page;
            await LoadUsersAsync();
        }

        private async Task AddUserAsync()
        {
            if (RequestUserEditorAsync is null)
            {
                return;
            }

            var input = await RequestUserEditorAsync(new UserEditorState());
            if (input is null)
            {
                return;
            }

            var result = await _userService.CreateAsync(new CreateUserInput
            {
                Username = input.Username,
                FullName = input.FullName,
                Password = input.Password ?? string.Empty,
                Role = "Sale"
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to create sales user.";
                return;
            }

            await LoadUsersAsync();
        }

        private async Task EditUserAsync(UserDto? user)
        {
            if (user is null || RequestUserEditorAsync is null)
            {
                return;
            }

            var input = await RequestUserEditorAsync(new UserEditorState
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role
            });

            if (input is null)
            {
                return;
            }

            var result = await _userService.UpdateAsync(user.UserId, new UpdateUserInput
            {
                Username = input.Username,
                FullName = input.FullName,
                Password = string.IsNullOrWhiteSpace(input.Password) ? null : input.Password,
                Role = "Sale"
            });

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to update sales user.";
                return;
            }

            await LoadUsersAsync();
        }

        private async Task DeleteUserAsync(UserDto? user)
        {
            if (user is null)
            {
                return;
            }

            if (ConfirmDeleteUserAsync is not null)
            {
                var confirmed = await ConfirmDeleteUserAsync(user);
                if (!confirmed)
                {
                    return;
                }
            }

            var result = await _userService.DeleteAsync(user.UserId);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to delete sales user.";
                return;
            }

            if (Users.Count == 1 && _currentPage > 1)
            {
                _currentPage--;
            }

            await LoadUsersAsync();
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
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                {
                    await LoadUsersAsync();
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
                _ = LoadUsersAsync();
            }
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

        private static int NormalizePageSize(int value)
        {
            return value is 5 or 10 or 15 or 20 ? value : DefaultPageSize;
        }

        private static DateTime ParseDate(string? value)
        {
            return DateTime.TryParse(value, out var parsed) ? parsed : DateTime.MinValue;
        }
    }

    public class UserEditorState
    {
        public string? UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Role { get; init; } = "Sale";
        public bool IsEditMode => !string.IsNullOrWhiteSpace(UserId);
    }

    public class UserEditorResult
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Password { get; set; }
    }
}
