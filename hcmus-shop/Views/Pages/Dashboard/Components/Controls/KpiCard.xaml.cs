using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace hcmus_shop.Views.Dashboard.Components.Controls
{
    public sealed partial class KpiCard : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(KpiCard), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(string), typeof(KpiCard), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DeltaTextProperty = DependencyProperty.Register(
            nameof(DeltaText), typeof(string), typeof(KpiCard), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
            nameof(Glyph), typeof(string), typeof(KpiCard), new PropertyMetadata("?"));

        public static readonly DependencyProperty IsPositiveProperty = DependencyProperty.Register(
            nameof(IsPositive), typeof(bool), typeof(KpiCard), new PropertyMetadata(true, OnIsPositiveChanged));

        public KpiCard()
        {
            InitializeComponent();
            DeltaBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 34, 197, 94));
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string DeltaText
        {
            get => (string)GetValue(DeltaTextProperty);
            set => SetValue(DeltaTextProperty, value);
        }

        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        public bool IsPositive
        {
            get => (bool)GetValue(IsPositiveProperty);
            set => SetValue(IsPositiveProperty, value);
        }

        public Brush DeltaBrush { get; private set; }

        private static void OnIsPositiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not KpiCard card)
            {
                return;
            }

            var isPositive = e.NewValue is bool value && value;
            card.DeltaBrush = isPositive
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 34, 197, 94))
                : new SolidColorBrush(ColorHelper.FromArgb(255, 244, 63, 94));
        }
    }
}
