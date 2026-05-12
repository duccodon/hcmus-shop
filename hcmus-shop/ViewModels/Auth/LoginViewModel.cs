using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace hcmus_shop.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private const string RememberedUsernameKey = "remembered_username";
        private readonly IAuthService _authService;
        private readonly IHealthService _healthService;

        public event EventHandler? LoginSucceeded;
        public event EventHandler? OpenConfigRequested;

        public LoginViewModel(IAuthService authService, IHealthService healthService)
        {
            _authService = authService;
            _healthService = healthService;
            LoadRememberedUsername();
        }

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string? usernameError;

        [ObservableProperty]
        private string? passwordError;

        [ObservableProperty]
        private bool rememberMe;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? errorMessage;

        // ---- Server status (from health probe) ----

        public enum ServerStatus { Unknown, Checking, Online, Offline }

        [ObservableProperty]
        private ServerStatus _server = ServerStatus.Unknown;

        [ObservableProperty]
        private string? _serverErrorReason;

        public bool IsServerOnline => Server == ServerStatus.Online;
        public bool IsServerChecking => Server == ServerStatus.Checking;
        public bool IsServerOffline => Server == ServerStatus.Offline;

        partial void OnServerChanged(ServerStatus value)
        {
            OnPropertyChanged(nameof(IsServerOnline));
            OnPropertyChanged(nameof(IsServerChecking));
            OnPropertyChanged(nameof(IsServerOffline));
            LoginCommand.NotifyCanExecuteChanged();
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool HasUsernameError => !string.IsNullOrWhiteSpace(UsernameError);
        public bool HasPasswordError => !string.IsNullOrWhiteSpace(PasswordError);

        partial void OnErrorMessageChanged(string? value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        partial void OnUsernameErrorChanged(string? value)
        {
            OnPropertyChanged(nameof(HasUsernameError));
        }

        partial void OnPasswordErrorChanged(string? value)
        {
            OnPropertyChanged(nameof(HasPasswordError));
        }

        partial void OnUsernameChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                UsernameError = null;
            }
        }

        partial void OnPasswordChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                PasswordError = null;
            }
        }

        /// <summary>
        /// Pings /health on the configured server URL. Called from LoginPage.Loaded.
        /// </summary>
        [RelayCommand]
        public async Task CheckServerAsync()
        {
            Server = ServerStatus.Checking;
            ServerErrorReason = null;

            var result = await _healthService.PingAsync();
            if (result.IsSuccess)
            {
                Server = ServerStatus.Online;
                ServerErrorReason = null;
            }
            else
            {
                Server = ServerStatus.Offline;
                ServerErrorReason = result.Error;
            }
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            if (IsBusy)
            {
                return;
            }

            ErrorMessage = null;
            UsernameError = null;
            PasswordError = null;

            if (!ValidateCredentials())
            {
                return;
            }

            IsBusy = true;
            try
            {
                var loginResult = await _authService.LoginAsync(Username.Trim(), Password, RememberMe);
                if (!loginResult.IsSuccess)
                {
                    ErrorMessage = loginResult.Error;
                    return;
                }

                SaveRememberedUsername();
                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool ValidateCredentials()
        {
            var isValid = true;

            if (string.IsNullOrWhiteSpace(Username))
            {
                UsernameError = "Username is required.";
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                PasswordError = "Password is required.";
                isValid = false;
            }

            return isValid;
        }

        [RelayCommand]
        private void OpenConfig()
        {
            OpenConfigRequested?.Invoke(this, EventArgs.Empty);
        }

        // Login is allowed only when not busy AND server is online.
        // (Server=Unknown is treated as "not yet verified" — login disabled until we know.)
        private bool CanLogin() => !IsBusy && Server == ServerStatus.Online;

        partial void OnIsBusyChanged(bool value)
        {
            LoginCommand.NotifyCanExecuteChanged();
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
