using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace SaveToGameWpf.Logic.Utils;

public static class StringUtils
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] [CanBeNull] this string str)
    {
        return string.IsNullOrEmpty(str);
    }
}