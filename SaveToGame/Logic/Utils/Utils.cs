using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using ICSharpCode.SharpZipLib.Zip;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Windows;

using Application = System.Windows.Application;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;
using IOHelper = UsefulFunctionsLib.UsefulFunctions_IOHelper;

namespace SaveToGameWpf.Logic.Utils
{
    internal static class Utils
    {
        public static string EncodeUnicode(string inputText)
        {
            var result = new StringBuilder(inputText.Length * 6);

            foreach (char symbol in inputText)
                result.Append($"\\u{$"{(int) symbol:x}".PadLeft(4, '0')}");

            return result.ToString();
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

        public static void ProVersionEnable(bool enable = false)
        {
            var mainWindow = WindowManager.GetActiveWindow<MainWindow>();

            Application.Current.Dispatcher.InvokeAction(() =>
            {
                if (!enable)
                    return;

                mainWindow.PopupBoxText.Value = DefaultSettingsContainer.Instance.PopupMessage ?? "";
                mainWindow.OnlySave.Value = true;
            });

            mainWindow.Pro.Value = enable;
        }

        public static void RunAsAdmin(string fileName, string anArguments)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = anArguments,
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(processInfo);
        }

        public static void InvokeAction(this Dispatcher dispatcher, Action action)
        {
            dispatcher.Invoke(action);
        }

        public static void OpenLinkInBrowser(string link)
        {
            try
            {
                Process.Start(link);
            }
            catch (Exception)
            {
                Process.Start(new ProcessStartInfo("IExplore.exe", link));
            }
        }

        public static void ExtractAll(this ZipFile zip, string folder)
        {
            IOHelper.DeleteFolder(folder);
            Directory.CreateDirectory(folder);

            foreach (ZipEntry entry in zip)
            {
                if (entry.IsDirectory)
                    continue;

                Directory.CreateDirectory(Path.Combine(folder, Path.GetDirectoryName(entry.Name) ?? string.Empty));

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

        public static bool SetProperty<TClass, TValue>(this TClass sender, ref TValue storage, TValue value, [CallerMemberName] string propertyName = null) where TClass : IRaisePropertyChanged
        {
            if (EqualityComparer<TValue>.Default.Equals(storage, value))
                return false;

            storage = value;
            sender.RaisePropertyChanged(propertyName);

            return true;
        }

        public static byte[] GetBytesUtf8(this string input)
        {
            return Encoding.UTF8.GetBytes(input);
        }

        public static T CloneTyped<T>(this T obj) where T : ICloneable
        {
            return (T) obj.Clone();
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

        public static DisposableUnion With(this IDisposable source, params IDisposable[] items)
        {
            var tmpItems = new IDisposable[items.Length + 1];

            tmpItems[0] = source;
            Array.Copy(items, 0, tmpItems, 1, items.Length);

            return new DisposableUnion(tmpItems);
        }
    }
}
