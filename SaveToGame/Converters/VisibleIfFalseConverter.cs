using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SaveToGameWpf.Converters
{
    class VisibleIfFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                throw new ArgumentException(@"Must be of 'bool' type", nameof(value));

            return (bool) value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Visibility))
                throw new ArgumentException(@"Must be of 'Visiblity' type", nameof(value));

            return (Visibility) value == Visibility.Collapsed;
        }
    }
}
