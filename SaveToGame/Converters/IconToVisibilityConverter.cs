using System.Globalization;
using System.Windows;
using MVVM_Tools.Code.Classes;

namespace SaveToGameWpf.Converters
{
    public class IconToVisibilityConverter : ConverterBase<object, Visibility>
    {
        public override Visibility ConvertInternal(object? value, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
