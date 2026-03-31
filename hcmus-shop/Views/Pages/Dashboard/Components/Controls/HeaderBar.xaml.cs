using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views.Dashboard.Components.Controls
{
    public sealed partial class HeaderBar : UserControl
    {
        public static readonly DependencyProperty WelcomeTextProperty = DependencyProperty.Register(
            nameof(WelcomeText), typeof(string), typeof(HeaderBar), new PropertyMetadata("Welcome"));

        public HeaderBar()
        {
            InitializeComponent();
        }

        public string WelcomeText
        {
            get => (string)GetValue(WelcomeTextProperty);
            set => SetValue(WelcomeTextProperty, value);
        }
    }
}
