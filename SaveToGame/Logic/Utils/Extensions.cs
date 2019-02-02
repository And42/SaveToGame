using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using LongPaths.Logic;

namespace SaveToGameWpf.Logic.Utils
{
    public static class Extensions
    {
        public static T As<T>(this object obj) => (T) obj;

        public static void ExtractAll(this ZipFile zip, string folder)
        {
            LDirectory.Delete(folder, true);
            LDirectory.CreateDirectory(folder);

            foreach (ZipEntry entry in zip)
            {
                if (entry.IsDirectory)
                    continue;

                LDirectory.CreateDirectory(Path.Combine(folder, Path.GetDirectoryName(entry.Name) ?? String.Empty));

                using (var zipStream = zip.GetInputStream(entry))
                using (var outputStream = LFile.Create(Path.Combine(folder, entry.Name)))
                {
                    zipStream.CopyTo(outputStream, 4096);
                }
            }
        }
    }
}
