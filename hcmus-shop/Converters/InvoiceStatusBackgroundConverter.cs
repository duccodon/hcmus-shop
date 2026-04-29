using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace hcmus_shop.Converters
{
    public class InvoiceStatusBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var status = value as string;
            var resourceKey = status switch
            {
                "Paid" => "SuccessBackgroundSubtle",
                "Pending" => "WarningBackgroundSubtle",
                "Cancelled" => "DangerBackgroundSubtle",
                _ => "DividerBrush",
            };

            return Application.Current.Resources[resourceKey] as Brush
                ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotSupportedException();
    }
}
