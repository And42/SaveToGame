using JetBrains.Annotations;
using LongPaths.Logic;

namespace SaveToGameWpf.Logic.Utils
{
    public static class FileUtils
    {
        public static long FileLength([NotNull] string path)
        {
            using (var stream = LFile.OpenRead(path))
                return stream.Length;
        }
    }
}