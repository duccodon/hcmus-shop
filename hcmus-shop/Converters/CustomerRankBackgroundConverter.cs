using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace hcmus_shop.Converters
{
    public class CustomerRankBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var color = value?.ToString() switch
            {
                "Diamond" => ColorHelper.FromArgb(0xFF, 0xDB, 0xEA, 0xFE),
                "Gold" => ColorHelper.FromArgb(0xFF, 0xFE, 0xF3, 0xC7),
                "Silver" => ColorHelper.FromArgb(0xFF, 0xF3, 0xF4, 0xF6),
                _ => ColorHelper.FromArgb(0xFF, 0xFF, 0xED, 0xD5)
            };

            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
