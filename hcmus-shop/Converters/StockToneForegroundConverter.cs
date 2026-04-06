using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace hcmus_shop.Converters
{
    public class StockToneForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var tone = value?.ToString();

            return tone switch
            {
                "Danger" => Application.Current.Resources["DangerForeground"] as Brush ?? new SolidColorBrush(ColorHelper.FromArgb(255, 220, 38, 38)),
                "Warning" => Application.Current.Resources["WarningForeground"] as Brush ?? new SolidColorBrush(ColorHelper.FromArgb(255, 161, 98, 7)),
                _ => new SolidColorBrush(Colors.Transparent),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
