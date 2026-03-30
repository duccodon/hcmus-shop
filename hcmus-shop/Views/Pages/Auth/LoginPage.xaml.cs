using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginViewModel ViewModel { get; }

        public LoginPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<LoginViewModel>();
            ViewModel.LoginSucceeded += OnLoginSucceeded;
            DataContext = ViewModel;
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Password = PasswordInput.Password;
        }

        private void OnLoginSucceeded(object? sender, System.EventArgs e)
        {
            if (Application.Current is App app)
            {
                app.OpenMainWindow();
            }
        }
    }
}
