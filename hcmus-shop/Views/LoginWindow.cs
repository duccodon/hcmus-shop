using Microsoft.UI.Xaml;
using hcmus_shop.Views;

namespace hcmus_shop
{
    public sealed class LoginWindow : Window
    {
        public LoginWindow()
        {
            Title = "MySuperShop";
            Content = new LoginPage();
        }
    }
}
