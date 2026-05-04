using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace hcmus_shop.Converters
{
    /// <summary>
    /// true → Error, false → Success.
    /// Used for InfoBar.Severity bound to an "isError" flag.
    /// </summary>
    public class BooleanToInfoSeverityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is true ? InfoBarSeverity.Error : InfoBarSeverity.Success;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
