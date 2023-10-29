using System;

namespace SaveToGameWpf.Logic.Utils
{
    internal static class Guard
    {
        public static void NotNullArgument<T>(T? argument, string argumentName) where T : class
        {
            if (argumentName == null)
                throw new ArgumentNullException(nameof(argumentName));

            if (argument == null)
                throw new ArgumentNullException(argumentName);
        }
    }
}
