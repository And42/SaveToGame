using System.Globalization;
using MVVM_Tools.Code.Classes;

namespace SaveToGameWpf.Converters
{
    public class DivideBy1024Converter : ConverterBase<long, long>
    {
        public override long ConvertInternal(long value, object parameter, CultureInfo culture)
        {
            return value / 1024;
        }
    }
}
