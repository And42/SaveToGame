using System;
using System.Globalization;
using MVVM_Tools.Code.Classes;

namespace SaveToGameWpf.Converters
{
    public class ThemeToCheckedConverter : ConverterBase<string, string, bool>
    {
        public override bool ConvertInternal(string currentTheme, string parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(parameter))
                throw new ArgumentException($"Unknown theme: \"{parameter}\"", nameof(parameter));

            if (parameter.Equals("light", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrEmpty(currentTheme) || currentTheme.Equals("light", StringComparison.OrdinalIgnoreCase);

            if (parameter.Equals("dark", StringComparison.OrdinalIgnoreCase))
                return currentTheme.Equals("dark", StringComparison.OrdinalIgnoreCase);
            
            throw new Exception($"Unknown theme in settings: \"{currentTheme}\"");
        }
    }
}
