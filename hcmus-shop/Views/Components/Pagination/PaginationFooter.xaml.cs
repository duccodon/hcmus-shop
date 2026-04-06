using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections;
using System.Windows.Input;

namespace hcmus_shop.Views.Components.Pagination
{
    public sealed partial class PaginationFooter : UserControl
    {
        public PaginationFooter()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ResultTextProperty = DependencyProperty.Register(
            nameof(ResultText),
            typeof(string),
            typeof(PaginationFooter),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty PageSizeOptionsProperty = DependencyProperty.Register(
            nameof(PageSizeOptions),
            typeof(IEnumerable),
            typeof(PaginationFooter),
            new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedPageSizeProperty = DependencyProperty.Register(
            nameof(SelectedPageSize),
            typeof(int),
            typeof(PaginationFooter),
            new PropertyMetadata(10));

        public static readonly DependencyProperty PageButtonsProperty = DependencyProperty.Register(
            nameof(PageButtons),
            typeof(IEnumerable),
            typeof(PaginationFooter),
            new PropertyMetadata(null));

        public static readonly DependencyProperty CenterTextProperty = DependencyProperty.Register(
            nameof(CenterText),
            typeof(string),
            typeof(PaginationFooter),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty PageButtonClickCommandProperty = DependencyProperty.Register(
            nameof(PageButtonClickCommand),
            typeof(ICommand),
            typeof(PaginationFooter),
            new PropertyMetadata(null));

        public string ResultText
        {
            get => (string)GetValue(ResultTextProperty);
            set => SetValue(ResultTextProperty, value);
        }

        public IEnumerable? PageSizeOptions
        {
            get => (IEnumerable?)GetValue(PageSizeOptionsProperty);
            set => SetValue(PageSizeOptionsProperty, value);
        }

        public int SelectedPageSize
        {
            get => (int)GetValue(SelectedPageSizeProperty);
            set => SetValue(SelectedPageSizeProperty, value);
        }

        public IEnumerable? PageButtons
        {
            get => (IEnumerable?)GetValue(PageButtonsProperty);
            set => SetValue(PageButtonsProperty, value);
        }

        public string CenterText
        {
            get => (string)GetValue(CenterTextProperty);
            set => SetValue(CenterTextProperty, value);
        }

        public ICommand? PageButtonClickCommand
        {
            get => (ICommand?)GetValue(PageButtonClickCommandProperty);
            set => SetValue(PageButtonClickCommandProperty, value);
        }
    }
}
