using System.Globalization;
using System.Windows;
using MVVM_Tools.Code.Classes;

namespace SaveToGameWpf.Converters
{
    public class FalseToHiddenConverter : ConverterBase<bool, Visibility>
    {
        public override Visibility ConvertInternal(bool value, object parameter, CultureInfo culture)
        {
            return value ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
