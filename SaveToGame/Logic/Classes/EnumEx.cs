using System;

namespace SaveToGameWpf.Logic.Classes
{
    internal static class EnumEx
    {
        public static TEnum Parse<TEnum>(string value)
        {
            return (TEnum) Enum.Parse(typeof(TEnum), value);
        }
    }
}
