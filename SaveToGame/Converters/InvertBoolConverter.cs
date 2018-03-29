using System;
using System.Globalization;
using System.Windows.Data;

namespace SaveToGameWpf.Converters
{
    class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                throw new ArgumentException(@"Must be of 'bool' type", nameof(value));

            return !(bool) value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                throw new ArgumentException(@"Must be of 'bool' type", nameof(value));

            return !(bool) value;
        }
    }
}
