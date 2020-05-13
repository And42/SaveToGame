using System.IO;
using JetBrains.Annotations;

namespace SaveToGameWpf.Logic.Utils
{
    public static class FileUtils
    {
        public static long FileLength([NotNull] string path)
        {
            using (var stream = File.OpenRead(path))
                return stream.Length;
        }
    }
}