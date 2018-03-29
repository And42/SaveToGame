using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SaveToGameWpf.Converters
{
    class VisibleIfTrueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                throw new ArgumentException(@"Must be of 'bool' type", nameof(value));

            return (bool) value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Visibility))
                throw new ArgumentException(@"Must be of 'Visibility' type", nameof(value));

            return (Visibility) value == Visibility.Visible;
        }
    }
}
