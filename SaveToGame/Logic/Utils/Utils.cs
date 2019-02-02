using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using LongPaths.Logic;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Resources.Localizations;
using SaveToGameWpf.Windows;

namespace SaveToGameWpf.Logic.Utils
{
    public class Utils
    {
        [NotNull] private readonly GlobalVariables _globalVariables;

        public Utils(
            [NotNull] GlobalVariables globalVariables
        )
        {
            _globalVariables = globalVariables;
        }

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

        public async Task DownloadJava(IVisualProgress visualProgress)
        {
            visualProgress.SetLabelText(MainResources.JavaDownloading);
            visualProgress.ShowIndeterminateLabel();

            bool fileDownloaded;

            const string jreUrl = @"https://storage.googleapis.com/savetogame/jre_1.7.zip";
            string fileLocation = Path.Combine(_globalVariables.AppDataPath, "jre.zip");

            LDirectory.CreateDirectory(_globalVariables.AppDataPath);

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
                    await Task.Factory.StartNew(() => zipFile.ExtractAll(_globalVariables.PathToPortableJre));
                }

                LFile.Delete(fileLocation);
            }

            visualProgress.HideIndeterminateLabel();
            visualProgress.SetLabelText(MainResources.AllDone);
        }
    }
}
