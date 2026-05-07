using Microsoft.UI.Xaml.Data;
using System;

namespace hcmus_shop.Converters
{
    /// <summary>
    /// Returns true if the string is not null/empty/whitespace.
    /// Useful for `IsOpen` of InfoBar bound to an optional message.
    /// </summary>
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !string.IsNullOrWhiteSpace(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
