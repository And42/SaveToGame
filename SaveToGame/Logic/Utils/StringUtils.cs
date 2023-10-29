using System.Diagnostics.CodeAnalysis;

namespace SaveToGameWpf.Logic.Utils;

public static class StringUtils
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? str)
    {
        return string.IsNullOrEmpty(str);
    }
}