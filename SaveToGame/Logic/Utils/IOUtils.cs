using Alphaleonis.Win32.Filesystem;

namespace SaveToGameWpf.Logic.Utils
{
    // ReSharper disable once UnusedMember.Global
    public static class IOUtils
    {
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
