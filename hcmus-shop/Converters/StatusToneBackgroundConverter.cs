using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace hcmus_shop.Converters
{
    public class StatusToneBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var tone = value?.ToString();

            return tone switch
            {
                "Published" or "Active" => Application.Current.Resources["SuccessBackgroundSubtle"] as Brush ?? new SolidColorBrush(ColorHelper.FromArgb(220, 220, 252, 231)),
                "Scheduled" => new SolidColorBrush(ColorHelper.FromArgb(255, 219, 234, 254)),
                "Expired" => Application.Current.Resources["WarningBackgroundSubtle"] as Brush ?? new SolidColorBrush(ColorHelper.FromArgb(220, 254, 243, 199)),
                "Inactive" => new SolidColorBrush(ColorHelper.FromArgb(255, 243, 244, 246)),
                "StockOut" => Application.Current.Resources["DangerBackgroundSubtle"] as Brush ?? new SolidColorBrush(ColorHelper.FromArgb(220, 254, 226, 226)),
                _ => Application.Current.Resources["WarningBackgroundSubtle"] as Brush ?? new SolidColorBrush(ColorHelper.FromArgb(220, 254, 243, 199)),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
