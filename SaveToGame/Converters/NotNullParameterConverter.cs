using System.Globalization;
using MVVM_Tools.Code.Classes;

namespace SaveToGameWpf.Converters
{
    public class NotNullParameterConverter : ConverterBase<object, object>
    {
        public override object ConvertInternal(object? value, object parameter, CultureInfo culture)
        {
            return value != null ? parameter : string.Empty;
        }
    }
}
