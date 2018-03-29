using System;
using System.Globalization;
using System.Windows.Data;

namespace SaveToGameWpf.Converters
{
    public class DivideBy1024Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is long))
                throw new ArgumentException(@"Must be of 'long' type", nameof(value));

            return (long)value / 1024;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
