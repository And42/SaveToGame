using System.IO;

namespace SaveToGameWpf.Logic.Utils
{
    public static class FileUtils
    {
        public static long FileLength(string path)
        {
            using (var stream = File.OpenRead(path))
                return stream.Length;
        }

        public static void CleanUpDirectory(string path)
        {
            if (!Directory.Exists(path))
                return;
            
            foreach (string file in Directory.EnumerateFiles(path))
                File.Delete(file);
            
            foreach (string directory in Directory.EnumerateDirectories(path))
                Directory.Delete(directory, recursive: true);
        }
    }
}