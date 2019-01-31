using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using LongPaths.Logic;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Resources.Localizations;
using SaveToGameWpf.Windows;

namespace SaveToGameWpf.Logic.Utils
{
    internal static class Utils
    {
        public static int CompareVersions(string first, string second)
        {
            return 
                CompareVersions(
                    Array.ConvertAll(first.Split('.'), int.Parse),
                    Array.ConvertAll(second.Split('.'), int.Parse)
                );
        }

        public static int CompareVersions(int[] first, int[] second)
        {
            int minLen = Math.Min(first.Length, second.Length);

            for (int i = 0; i < minLen; i++)
            {
                if (first[i] < second[i])
                    return -1;

                if (first[i] > second[i])
                    return 1;
            }

            return 0;
        }

        public static void ExtractAll(this ZipFile zip, string folder)
        {
            LDirectory.Delete(folder, true);
            LDirectory.CreateDirectory(folder);

            foreach (ZipEntry entry in zip)
            {
                if (entry.IsDirectory)
                    continue;

                LDirectory.CreateDirectory(Path.Combine(folder, Path.GetDirectoryName(entry.Name) ?? string.Empty));

                using (var zipStream = zip.GetInputStream(entry))
                using (var outputStream = LFile.Create(Path.Combine(folder, entry.Name)))
                {
                    zipStream.CopyTo(outputStream, 4096);
                }
            }
        }

        public static async Task DownloadJava(IVisualProgress visualProgress)
        {
            visualProgress.SetLabelText(MainResources.JavaDownloading);
            visualProgress.ShowIndeterminateLabel();

            bool fileDownloaded;

            const string jreUrl = @"https://storage.googleapis.com/savetogame/jre_1.7.zip";
            string fileLocation = Path.Combine(GlobalVariables.AppDataPath, "jre.zip");

            LDirectory.CreateDirectory(GlobalVariables.AppDataPath);

            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += (sender, args) => visualProgress.SetBarValue(args.ProgressPercentage);

                while (true)
                {
                    try
                    {
                        await client.DownloadFileTaskAsync(jreUrl, fileLocation);

                        fileDownloaded = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        var promt = MessBox.ShowDial(
                            string.Format(MainResources.JavaDownloadFailed, ex.Message),
                            MainResources.Error,
                            MainResources.No,
                            MainResources.Yes
                        );

                        if (promt == MainResources.Yes)
                            continue;

                        fileDownloaded = false;
                        break;
                    }
                }
            }

            if (fileDownloaded)
            {
                visualProgress.SetLabelText(MainResources.JavaExtracting);
                visualProgress.SetBarIndeterminate();

                using (var zipFile = new ZipFile(fileLocation))
                {
                    await Task.Factory.StartNew(() => zipFile.ExtractAll(GlobalVariables.PathToPortableJre));
                }

                LFile.Delete(fileLocation);
            }

            visualProgress.HideIndeterminateLabel();
            visualProgress.SetLabelText(MainResources.AllDone);
        }

        public static T As<T>(this object obj) => (T) obj;
    }
}
