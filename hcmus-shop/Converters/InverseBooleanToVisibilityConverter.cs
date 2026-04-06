using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace hcmus_shop.Converters
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is true ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility visibility && visibility != Visibility.Visible;
        }
    }
}
