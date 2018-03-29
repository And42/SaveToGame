using System;
using System.Globalization;
using System.Windows.Data;

namespace SaveToGameWpf.Converters
{
    public class NotNullParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? parameter : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
