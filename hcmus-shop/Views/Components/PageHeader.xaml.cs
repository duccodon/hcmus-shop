using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace hcmus_shop.Views.Components
{
    public sealed partial class PageHeader : UserControl
    {
        public PageHeader()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(PageHeader),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty PrimaryActionLabelProperty = DependencyProperty.Register(
            nameof(PrimaryActionLabel),
            typeof(string),
            typeof(PageHeader),
            new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ImportCommandProperty = DependencyProperty.Register(
            nameof(ImportCommand),
            typeof(ICommand),
            typeof(PageHeader),
            new PropertyMetadata(null));

        public static readonly DependencyProperty ExportCommandProperty = DependencyProperty.Register(
            nameof(ExportCommand),
            typeof(ICommand),
            typeof(PageHeader),
            new PropertyMetadata(null));

        public static readonly DependencyProperty PrimaryActionCommandProperty = DependencyProperty.Register(
            nameof(PrimaryActionCommand),
            typeof(ICommand),
            typeof(PageHeader),
            new PropertyMetadata(null));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string PrimaryActionLabel
        {
            get => (string)GetValue(PrimaryActionLabelProperty);
            set => SetValue(PrimaryActionLabelProperty, value);
        }

        public ICommand? ImportCommand
        {
            get => (ICommand?)GetValue(ImportCommandProperty);
            set => SetValue(ImportCommandProperty, value);
        }

        public ICommand? ExportCommand
        {
            get => (ICommand?)GetValue(ExportCommandProperty);
            set => SetValue(ExportCommandProperty, value);
        }

        public ICommand? PrimaryActionCommand
        {
            get => (ICommand?)GetValue(PrimaryActionCommandProperty);
            set => SetValue(PrimaryActionCommandProperty, value);
        }
    }
}
