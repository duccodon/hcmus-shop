using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views.Components
{
    public sealed partial class SearchFilterBar : UserControl
    {
        public SearchFilterBar()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(
            nameof(SearchText),
            typeof(string),
            typeof(SearchFilterBar),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FiltersContentProperty = DependencyProperty.Register(
            nameof(FiltersContent),
            typeof(object),
            typeof(SearchFilterBar),
            new PropertyMetadata(null));

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public object? FiltersContent
        {
            get => GetValue(FiltersContentProperty);
            set => SetValue(FiltersContentProperty, value);
        }
    }
}
