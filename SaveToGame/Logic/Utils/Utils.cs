using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using ICSharpCode.SharpZipLib.Zip;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Logic.OrganisationItems;
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

        public static void EnableProVersion(bool enable = false)
        {
            var model = WindowManager.GetActiveWindow<MainWindow>().ViewModel;

            model.Pro.Value = enable;

            if (!enable)
                return;

            model.PopupBoxText.Value = AppSettings.Instance.PopupMessage ?? "";
            model.OnlySave.Value = true;
        }

        public static void InvokeAction(this Dispatcher dispatcher, Action action)
        {
            dispatcher.Invoke(action);
        }

        public static void ExtractAll(this ZipFile zip, string folder)
        {
            IOUtils.RecreateDir(folder);

            foreach (ZipEntry entry in zip)
            {
                if (entry.IsDirectory)
                    continue;

                IOUtils.CreateDir(Path.Combine(folder, Path.GetDirectoryName(entry.Name) ?? string.Empty));

                using (var zipStream = zip.GetInputStream(entry))
                using (var outputStream = File.Create(Path.Combine(folder, entry.Name)))
                {
                    zipStream.CopyTo(outputStream, 4096);
                }
            }
        }

        // ReSharper disable once InconsistentNaming
        public static string GetFullFNWithoutExt(this FileInfo fileInfo)
        {
            return fileInfo.FullName.Remove(fileInfo.FullName.Length - fileInfo.Extension.Length);
        }

        /// <summary>
        /// Returns installed java version in a format of (primary, secondary) where result equals (-1, -1) if java was not found
        /// </summary>
        /// <returns>Java version or (-1, -1) if java was not found</returns>
        public static (int primary, int secondary) GetInstalledJavaVersion()
        {
            Process process;

            try
            {
                process = Process.Start(new ProcessStartInfo("java", "-version")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true
                });
            }
            catch (Exception)
            {
                return (-1, -1);
            }

            if (process == null)
                return (-1, -1);

            process.WaitForExit();

            string output = process.StandardError.ReadLine();

            var versionRegex = new Regex(@"\""(?<primary>\d+)\.(?<secondary>\d+)\.[^""]+\""");

            Match match = versionRegex.Match(output ?? string.Empty);

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!match.Success)
                return (-1, -1);

            return (int.Parse(match.Groups["primary"].Value), int.Parse(match.Groups["secondary"].Value));
        }

        public static async Task DownloadJava(IVisualProgress visualProgress)
        {
            visualProgress.SetLabelText(MainResources.JavaDownloading);
            visualProgress.ShowIndeterminateLabel();

            bool fileDownloaded;

            const string jreUrl = @"https://storage.googleapis.com/savetogame/jre_1.7.zip";
            string fileLocation = Path.Combine(GlobalVariables.AppSettingsDir, "jre.zip");

            IOUtils.CreateDir(GlobalVariables.AppSettingsDir);

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

                IOUtils.DeleteFile(fileLocation);
            }

            visualProgress.HideIndeterminateLabel();
            visualProgress.SetLabelText(MainResources.AllDone);
        }

        public static T As<T>(this object obj) => (T) obj;
    }
}
