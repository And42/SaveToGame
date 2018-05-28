using System.Globalization;
using MVVM_Tools.Code.Classes;

namespace SaveToGameWpf.Converters
{
    public class ByteToMbConverter : ConverterBase<long, long>
    {
        public override long ConvertInternal(long value, object parameter, CultureInfo culture)
        {
            return value / 1024 / 1024;
        }
    }
}
