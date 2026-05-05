using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace hcmus_shop.Converters
{
    public class InvoiceLegendMarkerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var label = value as string;
            var resourceKey = label switch
            {
                "Paid" => "AccentCyan",
                "Overdue" => "AccentPurple",
                "Unpaid" => "AccentOrange",
                "Draft" => "TextSecondary",
                _ => "TextSecondary",
            };

            return Application.Current.Resources[resourceKey] as Brush
                ?? new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotSupportedException();
    }
}
