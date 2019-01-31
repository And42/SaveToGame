using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace SaveToGameWpf.Logic.LongPaths
{
    public static class LFile
    {
        [NotNull]
        public static FileStream Create([NotNull] string path)
        {
            return File.Create(path);
        }

        [NotNull]
        public static FileStream OpenRead([NotNull] string path)
        {
            return File.OpenRead(path);
        }

        public static void Delete([NotNull] string path)
        {
            File.Delete(path);
        }

        public static void Copy([NotNull] string sourceFileName, [NotNull] string destFileName)
        {
            File.Copy(sourceFileName, destFileName);
        }

        public static void Copy([NotNull] string sourceFileName, [NotNull] string destFileName, bool overwrite)
        {
            File.Copy(sourceFileName, destFileName, overwrite);
        }

        public static void Move([NotNull] string sourceFileName, [NotNull] string destFileName)
        {
            File.Move(sourceFileName, destFileName);
        }

        public static bool Exists([CanBeNull] string path)
        {
            return File.Exists(path);
        }

        [NotNull]
        public static byte[] ReadAllBytes([NotNull] string path)
        {
            return File.ReadAllBytes(path);
        }

        [NotNull]
        public static string ReadAllText([NotNull] string path)
        {
            return File.ReadAllText(path);
        }

        [NotNull]
        public static string ReadAllText([NotNull] string path, [NotNull] Encoding encoding)
        {
            return File.ReadAllText(path, encoding);
        }

        public static void WriteAllBytes([NotNull] string path, [NotNull] byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
        }

        public static void WriteAllText([NotNull] string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public static void WriteAllText([NotNull] string path, string contents, [NotNull] Encoding encoding)
        {
            File.WriteAllText(path, contents, encoding);
        }

        public static void SetLastWriteTime([NotNull] string path, DateTime lastWriteTime)
        {
            File.SetLastWriteTime(path, lastWriteTime);
        }
    }
}
