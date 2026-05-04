using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Contracts.Services;
using hcmus_shop.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace hcmus_shop.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginViewModel ViewModel { get; }
        public string VersionText { get; }
        public string ServerUrlText { get; }

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
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Password = PasswordInput.Password;
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
    }
}
