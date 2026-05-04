using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using hcmus_shop.Views;
using System;

namespace hcmus_shop
{
    /// <summary>
    /// The pre-login window. Hosts a Frame that swaps between LoginPage and ConfigPage.
    /// LoginPage raises an event to navigate to ConfigPage; ConfigPage raises an event
    /// to navigate back. This avoids a global app-router for two screens.
    /// </summary>
    public sealed class LoginWindow : Window
    {
        private readonly Frame _frame;

        public LoginWindow()
        {
            Title = "HCMUS Shop";
            _frame = new Frame();
            Content = _frame;
            ShowLoginPage();
        }

        private void ShowLoginPage()
        {
            _frame.Navigate(typeof(LoginPage));
            if (_frame.Content is LoginPage page)
            {
                page.ConfigRequested += OnConfigRequested;
            }
        }

        private void OnConfigRequested(object? sender, EventArgs e)
        {
            _frame.Navigate(typeof(ConfigPage));
            if (_frame.Content is ConfigPage page)
            {
                page.Saved += OnConfigDoneAndReturn;
                page.Cancelled += OnConfigDoneAndReturn;
            }
        }

        private void OnConfigDoneAndReturn(object? sender, EventArgs e)
        {
            ShowLoginPage();
        }
    }
}
