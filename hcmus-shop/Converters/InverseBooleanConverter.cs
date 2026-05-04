using Microsoft.UI.Xaml.Data;
using System;

namespace hcmus_shop.Converters
{
    /// <summary>
    /// Inverts a boolean. Useful for IsEnabled = !IsBusy.
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is bool b ? !b : true;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => value is bool b ? !b : false;
    }
}
