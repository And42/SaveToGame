using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace SaveToGameWpf.Logic.LongPaths
{
    public static class LDirectory
    {
        public static bool Exists([CanBeNull] string path)
        {
            return Directory.Exists(path);
        }

        public static void Delete([NotNull] string path)
        {
            Directory.Delete(path);
        }

        public static void Delete([NotNull] string path, bool recursive)
        {
            Directory.Delete(path, recursive);
        }

        public static void CreateDirectory([NotNull] string path)
        {
            Directory.CreateDirectory(path);
        }

        [NotNull]
        public static string[] GetFiles([NotNull] string path)
        {
            return Directory.GetFiles(path);
        }

        [NotNull]
        public static string[] GetFiles([NotNull] string path, [NotNull] string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern);
        }

        [NotNull]
        public static string[] GetFiles([NotNull] string path, [NotNull] string searchPattern, [NotNull] SearchOption searchOption)
        {
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        [NotNull]
        public static IEnumerable<string> EnumerateFiles([NotNull] string path)
        {
            return Directory.EnumerateFiles(path);
        }

        [NotNull]
        public static IEnumerable<string> EnumerateFiles([NotNull] string path, [NotNull] string searchPattern)
        {
            return Directory.EnumerateFiles(path, searchPattern);
        }

        [NotNull]
        public static IEnumerable<string> EnumerateFiles([NotNull] string path, [NotNull] string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateFiles(path, searchPattern, searchOption);
        }

        [NotNull]
        public static IEnumerable<string> EnumerateDirectories([NotNull] string path)
        {
            return Directory.EnumerateDirectories(path);
        }

        [NotNull]
        public static IEnumerable<string> EnumerateDirectories([NotNull] string path, [NotNull] string searchPattern)
        {
            return Directory.EnumerateDirectories(path, searchPattern);
        }

        [NotNull]
        public static IEnumerable<string> EnumerateDirectories([NotNull] string path, [NotNull] string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateDirectories(path, searchPattern, searchOption);
        }

        [NotNull]
        public static IEnumerable<string> EnumerateFileSystemEntries([NotNull] string path)
        {
            return Directory.EnumerateFileSystemEntries(path);
        }

        [NotNull]
        public static IEnumerable<string> EnumerateFileSystemEntries([NotNull] string path, [NotNull] string searchPattern)
        {
            return Directory.EnumerateFileSystemEntries(path, searchPattern);
        }

        [NotNull]
        public static IEnumerable<string> EnumerateFileSystemEntries([NotNull] string path, [NotNull] string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateFileSystemEntries(path, searchPattern, searchOption);
        }
    }
}
