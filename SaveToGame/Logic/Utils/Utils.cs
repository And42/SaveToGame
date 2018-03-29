using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Threading;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.WindowsAPICodePack.Dialogs;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Windows;

using Application = System.Windows.Application;
using DataFormats = System.Windows.DataFormats;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;
using IOHelper = UsefulFunctionsLib.UsefulFunctions_IOHelper;

namespace SaveToGameWpf.Logic.Utils
{
    internal static class Utils
    {
        private static class ComputerInfo
        {
            public static string DiskDrive => "Win32_DiskDrive";
            public static string Processor => "Win32_Processor";
            public static string SystemProduct => "Win32_ComputerSystemProduct";
            // ReSharper disable once InconsistentNaming
            public static string CDROM => "Win32_CDROMDrive";
            public static string Card => "CIM_Card";

            /// <summary>
            /// Returns all management objects from the provided place
            /// </summary>
            /// <param name="from">The place to selectfrom</param>
            public static List<ManagementObject> GetQueryList(string from)
            {
                var winQuery = new ObjectQuery("SELECT * FROM " + from);
                var searcher = new ManagementObjectSearcher(winQuery);

                return searcher.Get().Cast<ManagementObject>().ToList();
            }
        }

        public static (List<ManagementObject> systemObjects, string queryPropertyName)[] GetQueries()
        {
            (string from, string propName, Predicate<ManagementObject> checker)[] items =
            {
                (ComputerInfo.CDROM, "DeviceID", _ => true),
                (ComputerInfo.Card, "SerialNumber", _ => true),
                (ComputerInfo.DiskDrive, "SerialNumber", it => it["MediaType"]?.ToString() == "Fixed hard disk media"),
                (ComputerInfo.Processor, "ProcessorId", _ => true),
                (ComputerInfo.SystemProduct, "UUID", _ => true)
            };

            return Array.ConvertAll(items, item =>
            {
                var queries = ComputerInfo.GetQueryList(item.from);
                return
                    (
                    queries.FindAll(it => item.checker(it) && !string.IsNullOrEmpty(it[item.propName]?.ToString())),
                    item.propName
                    );
            });
        }

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
                    first.Split('.').Select(int.Parse).ToArray(),
                    second.Split('.').Select(int.Parse).ToArray()
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
                if (enable)
                {
                    mainWindow.PopupBoxText = SettingsIncapsuler.PopupMessage ?? "";
                    mainWindow.OnlySave = true;
                }
            });

            mainWindow.Pro = enable;
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

        public static void CheckDragOver(DragEventArgs e, params string[] extensions)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files?.Length == 1 && extensions.Any(ext => files[0].EndsWith(ext, StringComparison.Ordinal)))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
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

        public static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public static void DeleteFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, true);
        }

        public static bool SetProperty<TClass, TValue>(this TClass sender, ref TValue storage, TValue value, [CallerMemberName] string propertyName = null) where TClass : IRaisePropertyChanged
        {
            if (EqualityComparer<TValue>.Default.Equals(storage, value))
                return false;

            storage = value;
            sender.RaisePropertyChanged(propertyName);

            return true;
        }

        public static (bool success, string folderPath) OpenFolderWithDialog(string title = null)
        {
            // XP
            if (Environment.OSVersion.Version.Major < 6)
            {
                var dialog = new FolderBrowserDialog
                {
                    Description = title
                };

                return dialog.ShowDialog() == DialogResult.OK ? (true, dialog.SelectedPath) : (false, null);
            }
            else
            {
                var dialog = new CommonOpenFileDialog
                {
                    Title = title,
                    IsFolderPicker = true,
                    Multiselect = false
                };

                return dialog.ShowDialog() == CommonFileDialogResult.Ok ? (true, dialog.FileName) : (false, null);
            }
        }
    }
}
