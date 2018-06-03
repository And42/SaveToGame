using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SaveToGameWpf.Logic.Utils
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class IOUtils
    {
        public static void RecreateDir(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            else if (File.Exists(path))
                File.Delete(path);

            Directory.CreateDirectory(path);
        }

        public static void CreateDir(string path)
        {
            if (Directory.Exists(path))
                return;
            
            DeleteFile(path);

            Directory.CreateDirectory(path);
        }

        public static void DeleteDir(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public static void DeleteFileOrDir(string path)
        {
            DeleteDir(path);
            DeleteDir(path);
        }
    }
}
