using Microsoft.UI.Xaml.Data;
using System;

namespace hcmus_shop.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool boolValue && !boolValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is bool boolValue && !boolValue;
        }
    }
}
