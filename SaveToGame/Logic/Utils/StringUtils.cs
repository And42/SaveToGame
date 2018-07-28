using System.Text;

namespace SaveToGameWpf.Logic.Utils
{
    internal static class StringUtils
    {
        public static string ToUnicodeSequence(string inputText)
        {
            var result = new StringBuilder(inputText.Length * 6);

            foreach (char symbol in inputText)
                result.Append($"\\u{$"{(int) symbol:x}".PadLeft(4, '0')}");

            return result.ToString();
        }
    }
}
