using System.Globalization;
using MVVM_Tools.Code.Classes;
using SharedData.Enums;

namespace SaveToGameWpf.Converters
{
    public class BackupToCheckedConverter : ConverterBase<BackupType, BackupType, bool>
    {
        public override bool ConvertInternal(BackupType value, BackupType parameter, CultureInfo culture)
        {
            return value == parameter;
        }

        public override BackupType ConvertBackInternal(bool value, BackupType parameter, CultureInfo culture)
        {
            if (!value)
            {
                // just ignore as this happens when user clicks on the currently selected option
                return parameter;
            }

            return parameter;
        }
    }
}
