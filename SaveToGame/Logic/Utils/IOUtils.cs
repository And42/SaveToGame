using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using JetBrains.Annotations;

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

        [NotNull]
        public static FileStream FileOpenRead([NotNull] string path)
        {
            Guard.NotNullArgument(path, nameof(path));

            return File.OpenRead(path);
        }

        [NotNull]
        public static FileStream FileCreate([NotNull] string path)
        {
            Guard.NotNullArgument(path, nameof(path));

            return File.Create(path);
        }

        public static void FileWriteAllText(
            [NotNull] string path,
            [NotNull] string contents
        )
        {
            Guard.NotNullArgument(path, nameof(path));
            Guard.NotNullArgument(contents, nameof(contents));

            FileWriteAllText(path, contents, Encoding.UTF8);
        }

        public static void FileWriteAllText(
            [NotNull] string path,
            [NotNull] string contents,
            [NotNull] Encoding encoding
        )
        {
            Guard.NotNullArgument(path, nameof(path));
            Guard.NotNullArgument(contents, nameof(contents));
            Guard.NotNullArgument(encoding, nameof(encoding));

            File.WriteAllText(path, contents, encoding);
        }

        public static void FileDelete([NotNull] string filePath)
        {
            Guard.NotNullArgument(filePath, nameof(filePath));

            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public static void FolderDelete([NotNull] string folderPath)
        {
            Guard.NotNullArgument(folderPath, nameof(folderPath));

            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, true);
        }

        public static void FolderCreate([NotNull] string path)
        {
            Guard.NotNullArgument(path, nameof(path));

            Directory.CreateDirectory(path);
        }

        public static bool FileExists([NotNull] string path)
        {
            Guard.NotNullArgument(path, nameof(path));

            return File.Exists(path);
        }

        public static bool FolderExists([NotNull] string path)
        {
            Guard.NotNullArgument(path, nameof(path));

            return Directory.Exists(path);
        }

        public static void FileCopy(
            [NotNull] string source,
            [NotNull] string target,
            bool overwrite = true
        )
        {
            Guard.NotNullArgument(source, nameof(source));
            Guard.NotNullArgument(target, nameof(target));

            File.Copy(source, target, overwrite);
        }
    }
}
