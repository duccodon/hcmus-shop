using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Customers.Dto;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Customers
{
    public class CustomerDetailViewModel : ObservableObject
    {
        private readonly ICustomerService _customerService;
        private readonly IAuthService _authService;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private string _statusMessage = string.Empty;
        private string _customerId = string.Empty;
        private string _name = string.Empty;
        private string _phone = string.Empty;
        private string _email = string.Empty;
        private int _loyaltyPoints;
        private string _rank = "Bronze";
        private string _createdAt = string.Empty;
        private string _updatedAt = string.Empty;

        public CustomerDetailViewModel(ICustomerService customerService, IAuthService authService)
        {
            _customerService = customerService;
            _authService = authService;

            SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanEdit && !IsLoading);
            DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => CanEdit && !IsLoading);
            BackCommand = new RelayCommand(RequestBackNavigation);
        }

        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand DeleteCommand { get; }
        public IRelayCommand BackCommand { get; }
        public Func<Task<bool>>? ConfirmDeleteAsync { get; set; }
        public Action? GoBackRequested { get; set; }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    SaveCommand.NotifyCanExecuteChanged();
                    DeleteCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool CanEdit => _authService.HasRole("Admin") && !string.IsNullOrWhiteSpace(CustomerId);

        public string CustomerId
        {
            get => _customerId;
            private set
            {
                if (SetProperty(ref _customerId, value))
                {
                    OnPropertyChanged(nameof(CanEdit));
                    SaveCommand.NotifyCanExecuteChanged();
                    DeleteCommand.NotifyCanExecuteChanged();
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

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (SetProperty(ref _statusMessage, value))
                {
                    OnPropertyChanged(nameof(HasStatusMessage));
                }
            }
        }

        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public int LoyaltyPoints
        {
            get => _loyaltyPoints;
            private set => SetProperty(ref _loyaltyPoints, value);
        }

        public string Rank
        {
            get => _rank;
            private set => SetProperty(ref _rank, value);
        }

        public string CreatedAt
        {
            get => _createdAt;
            private set => SetProperty(ref _createdAt, value);
        }

        public string UpdatedAt
        {
            get => _updatedAt;
            private set => SetProperty(ref _updatedAt, value);
        }

        public async Task LoadAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
            {
                ErrorMessage = "Customer id is required.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;

            try
            {
                var result = await _customerService.GetByIdAsync(customerId);
                if (!result.IsSuccess || result.Value is null)
                {
                    ErrorMessage = result.Error ?? "Failed to load customer.";
                    return;
                }

                ApplyCustomer(result.Value);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveAsync()
        {
            if (!CanEdit)
            {
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;

            try
            {
                var result = await _customerService.UpdateAsync(CustomerId, new UpdateCustomerInput
                {
                    Name = Name.Trim(),
                    Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim()
                });

                if (!result.IsSuccess || result.Value is null)
                {
                    ErrorMessage = result.Error ?? "Failed to update customer.";
                    return;
                }

                ApplyCustomer(result.Value);
                StatusMessage = "Customer profile updated.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteAsync()
        {
            if (!CanEdit)
            {
                return;
            }

            if (ConfirmDeleteAsync is not null)
            {
                var confirmed = await ConfirmDeleteAsync();
                if (!confirmed)
                {
                    return;
                }
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;

            try
            {
                var result = await _customerService.DeleteAsync(CustomerId);
                if (!result.IsSuccess)
                {
                    ErrorMessage = result.Error ?? "Failed to delete customer.";
                    return;
                }

                RequestBackNavigation();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyCustomer(CustomerDto customer)
        {
            CustomerId = customer.CustomerId;
            Name = customer.Name;
            Phone = customer.Phone ?? string.Empty;
            Email = customer.Email ?? string.Empty;
            LoyaltyPoints = customer.LoyaltyPoints;
            Rank = customer.Rank;
            CreatedAt = FormatDate(customer.CreatedAt);
            UpdatedAt = FormatDate(customer.UpdatedAt);
        }

        private void RequestBackNavigation()
        {
            GoBackRequested?.Invoke();
        }

        private static string FormatDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "N/A";
            }

            return DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                    out var parsed)
                ? parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : value;
        }
    }
}
