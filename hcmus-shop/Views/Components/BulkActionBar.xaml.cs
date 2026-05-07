using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace hcmus_shop.Views.Components
{
    public sealed partial class BulkActionBar : UserControl
    {
        public BulkActionBar()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(
            nameof(IsVisible),
            typeof(bool),
            typeof(BulkActionBar),
            new PropertyMetadata(false));

        public static readonly DependencyProperty SelectionTextProperty = DependencyProperty.Register(
            nameof(SelectionText),
            typeof(string),
            typeof(BulkActionBar),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ToggleStatusCommandProperty = DependencyProperty.Register(
            nameof(ToggleStatusCommand),
            typeof(ICommand),
            typeof(BulkActionBar),
            new PropertyMetadata(null));

        public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register(
            nameof(DeleteCommand),
            typeof(ICommand),
            typeof(BulkActionBar),
            new PropertyMetadata(null));

        public bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        public string SelectionText
        {
            get => (string)GetValue(SelectionTextProperty);
            set => SetValue(SelectionTextProperty, value);
        }

        public ICommand? ToggleStatusCommand
        {
            get => (ICommand?)GetValue(ToggleStatusCommandProperty);
            set => SetValue(ToggleStatusCommandProperty, value);
        }

        public ICommand? DeleteCommand
        {
            get => (ICommand?)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }
    }
}
