using System;
using JetBrains.Annotations;

namespace SaveToGameWpf.Logic.Utils
{
    internal static class Guard
    {
        public static void NotNullArgument<T>([CanBeNull] T argument, [NotNull] string argumentName) where T : class
        {
            if (argumentName == null)
                throw new ArgumentNullException(nameof(argumentName));

            if (argument == null)
                throw new ArgumentNullException(argumentName);
        }
    }
}
