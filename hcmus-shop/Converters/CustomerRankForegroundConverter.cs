using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace hcmus_shop.Converters
{
    public class CustomerRankForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var color = value?.ToString() switch
            {
                "Diamond" => ColorHelper.FromArgb(0xFF, 0x1D, 0x4E, 0x89),
                "Gold" => ColorHelper.FromArgb(0xFF, 0x85, 0x4D, 0x0E),
                "Silver" => ColorHelper.FromArgb(0xFF, 0x37, 0x41, 0x51),
                _ => ColorHelper.FromArgb(0xFF, 0x9A, 0x34, 0x12)
            };

            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
