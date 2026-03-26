using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Services;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace hcmus_shop.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private const string RememberedUsernameKey = "remembered_username";
        private readonly IAuthService _authService;

        public event EventHandler? LoginSucceeded;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            LoadRememberedUsername();
        }

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool rememberMe;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        partial void OnErrorMessageChanged(string value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter username and password.";
                return;
            }

            var isLoggedIn = await _authService.LoginAsync(Username.Trim(), Password);
            if (!isLoggedIn)
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            SaveRememberedUsername();
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Update()
        {
            ErrorMessage = "Use Config to update environment settings.";
        }

        [RelayCommand]
        private void OpenConfig()
        {
            ErrorMessage = "Config screen is not implemented yet.";
        }

        private void LoadRememberedUsername()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(RememberedUsernameKey, out var value)
                && value is string rememberedUsername
                && !string.IsNullOrWhiteSpace(rememberedUsername))
            {
                Username = rememberedUsername;
                RememberMe = true;
            }
        }

        private void SaveRememberedUsername()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (RememberMe && !string.IsNullOrWhiteSpace(Username))
            {
                localSettings.Values[RememberedUsernameKey] = Username.Trim();
                return;
            }

            localSettings.Values.Remove(RememberedUsernameKey);
        }
    }
}
