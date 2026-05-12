using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Contracts.Services;
using hcmus_shop.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.System;

namespace hcmus_shop.Views
{
    public sealed partial class LoginPage : Page
    {
        private bool _isPasswordVisible;
        private bool _isSynchronizingPasswordInputs;

        public LoginViewModel ViewModel { get; }
        public string VersionText { get; }
        public string ServerUrlText { get; }
        public string LicenseText { get; }

        /// <summary>
        /// Raised when the user clicks the Config button.
        /// LoginWindow listens and swaps the frame to ConfigPage.
        /// </summary>
        public event EventHandler? ConfigRequested;

        public LoginPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<LoginViewModel>();
            ViewModel.LoginSucceeded += OnLoginSucceeded;
            ViewModel.OpenConfigRequested += OnOpenConfigRequested;
            DataContext = ViewModel;

            var config = Ioc.Default.GetRequiredService<IConfiguration>();
            VersionText = $"v{config["AppSettings:AppVersion"] ?? "1.0.0"}";

            var graphQL = Ioc.Default.GetRequiredService<IGraphQLClientService>();
            ServerUrlText = $"Server: {graphQL.ServerUrl}";

            var license = Ioc.Default.GetRequiredService<ILicenseService>();
            LicenseText = license.IsLicensed
                ? $"Licensed — {license.DaysRemaining} day(s) left"
                : $"Trial — {license.DaysRemaining} day(s) left";

            // Pre-flight health check when the page becomes visible.
            // Disables the Sign In button if the server can't be reached.
            Loaded += async (_, _) => await ViewModel.CheckServerAsync();
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSynchronizingPasswordInputs)
            {
                return;
            }

            _isSynchronizingPasswordInputs = true;
            PasswordRevealInput.Text = PasswordInput.Password;
            _isSynchronizingPasswordInputs = false;
            ViewModel.Password = PasswordInput.Password;
        }

        private void PasswordRevealInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSynchronizingPasswordInputs)
            {
                return;
            }

            _isSynchronizingPasswordInputs = true;
            PasswordInput.Password = PasswordRevealInput.Text;
            _isSynchronizingPasswordInputs = false;
            ViewModel.Password = PasswordRevealInput.Text;
        }

        private void PasswordInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter)
            {
                return;
            }

            if (ViewModel.LoginCommand.CanExecute(null))
            {
                ViewModel.LoginCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void OnLoginSucceeded(object? sender, EventArgs e)
        {
            if (Application.Current is App app)
            {
                app.OpenMainWindow();
            }
        }

        private void OnOpenConfigRequested(object? sender, EventArgs e)
        {
            ConfigRequested?.Invoke(this, EventArgs.Empty);
        }

        private void PasswordRevealButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                PasswordRevealInput.Text = PasswordInput.Password;
                PasswordInput.Visibility = Visibility.Collapsed;
                PasswordRevealInput.Visibility = Visibility.Visible;
                PasswordRevealInput.Focus(FocusState.Programmatic);
                PasswordRevealInput.SelectionStart = PasswordRevealInput.Text.Length;
                PasswordRevealIcon.Glyph = "\uE891";
                return;
            }

            PasswordInput.Password = PasswordRevealInput.Text;
            PasswordRevealInput.Visibility = Visibility.Collapsed;
            PasswordInput.Visibility = Visibility.Visible;
            PasswordInput.Focus(FocusState.Programmatic);
            PasswordRevealIcon.Glyph = "\uE890";
        }
    }
}
