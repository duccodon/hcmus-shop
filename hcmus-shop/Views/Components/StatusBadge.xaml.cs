using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views.Components
{
    public sealed partial class StatusBadge : UserControl
    {
        public StatusBadge()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty StatusTextProperty = DependencyProperty.Register(
            nameof(StatusText),
            typeof(string),
            typeof(StatusBadge),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty StatusToneProperty = DependencyProperty.Register(
            nameof(StatusTone),
            typeof(string),
            typeof(StatusBadge),
            new PropertyMetadata("Inactive"));

        public string StatusText
        {
            get => (string)GetValue(StatusTextProperty);
            set => SetValue(StatusTextProperty, value);
        }

        public string StatusTone
        {
            get => (string)GetValue(StatusToneProperty);
            set => SetValue(StatusToneProperty, value);
        }
    }
}
