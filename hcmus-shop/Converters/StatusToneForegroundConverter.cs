using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace hcmus_shop.Converters
{
    public class StatusToneForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var tone = value?.ToString();

            return tone switch
            {
                "Published" => Application.Current.Resources["SuccessForeground"] as Brush ?? new SolidColorBrush(ColorHelper.FromArgb(255, 21, 128, 61)),
                "Inactive" => Application.Current.Resources["DangerForeground"] as Brush ?? new SolidColorBrush(ColorHelper.FromArgb(255, 185, 28, 28)),
                _ => Application.Current.Resources["WarningForeground"] as Brush ?? new SolidColorBrush(ColorHelper.FromArgb(255, 161, 98, 7)),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
