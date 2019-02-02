using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AndroidHelper.Interfaces;
using AndroidHelper.Logic;
using AndroidHelper.Logic.Interfaces;
using AndroidHelper.Logic.SharpZip;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Interfaces.Enums;
using JetBrains.Annotations;
using LongPaths.Logic;
using SaveToGameWpf.Logic.Utils;
using TempUtils = AndroidHelper.Logic.Utils.TempUtils;

namespace SaveToGameWpf.Logic.Classes
{
    public static class ApkModifer
    {
        public static void ParseBackup(
            [NotNull] string pathToBackup,
            BackupType backupType,
            [NotNull] string resultInternalDataPath,
            [NotNull] string resultExternalDataPath,
            //[NotNull] string resultObbPath,
            [NotNull] ITempFolderProvider tempFolderProvider
        )
        {
            Guard.NotNullArgument(pathToBackup, nameof(pathToBackup));
            Guard.NotNullArgument(resultInternalDataPath, nameof(resultInternalDataPath));
            Guard.NotNullArgument(resultExternalDataPath, nameof(resultExternalDataPath));
            //Guard.NotNullArgument(resultObbPath, nameof(resultObbPath));
            Guard.NotNullArgument(tempFolderProvider, nameof(tempFolderProvider));

            switch (backupType)
            {
                case BackupType.Titanium:
                    {
                        #region Структура

                        /*

                            data
                                data
                                    .external.appname
                                        .
                                            данные 
                                    appname
                                        .
                                            данные
                        */

                        #endregion

                        using (var extractedBackup = TempUtils.UseTempFolder(tempFolderProvider))
                        {
                            ExtractTarByEntry(pathToBackup, extractedBackup.TempFolder);

                            string path = Path.Combine(extractedBackup.TempFolder, "data", "data");

                            foreach (string dir in LDirectory.EnumerateDirectories(path))
                            {
                                string dirName = Path.GetFileName(dir);
                                if (string.IsNullOrEmpty(dirName))
                                    continue;

                                switch (dirName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)[0])
                                {
                                    case "external obb":
                                        //CreateArchive(dir, resultObbPath);
                                        break;
                                    case "external":
                                        CreateArchive(dir, resultExternalDataPath);
                                        break;
                                    default:
                                        CreateArchive(dir, resultInternalDataPath);
                                        break;
                                }
                            }
                        }

                        break;
                    }
                case BackupType.RomToolbox:
                    {
                        #region Структура

                        /*

                            data
                                data
                                    appname
                                        данные
                            storage\emulated\legacy
                                Android
                                    data
                                        appname
                                            данные

                        */

                        #endregion

                        using (var extractedBackup = TempUtils.UseTempFolder(tempFolderProvider))
                        {
                            ExtractTarByEntry(pathToBackup, extractedBackup.TempFolder);

                            IEnumerable<string> firstLevelDirs = LDirectory.EnumerateDirectories(extractedBackup.TempFolder);

                            foreach (string firstLevelDir in firstLevelDirs)
                            {
                                if (Path.GetFileName(firstLevelDir) == "data")
                                {
                                    string path = Path.Combine(firstLevelDir, "data");

                                    string dir = LDirectory.EnumerateDirectories(path).FirstOrDefault();
                                    if (dir == default)
                                        continue;

                                    CreateArchive(dir, Path.Combine(resultInternalDataPath));
                                }
                                else
                                {
                                    string dir = LDirectory.EnumerateDirectories(firstLevelDir, "Android", SearchOption.AllDirectories).FirstOrDefault();
                                    if (dir == default)
                                        continue;

                                    string path = Path.Combine(dir, "data");

                                    string dir2 = LDirectory.EnumerateDirectories(path).FirstOrDefault();
                                    if (dir2 == default)
                                        continue;

                                    CreateArchive(dir2, resultExternalDataPath);
                                }
                            }
                        }

                        break;
                    }
                case BackupType.LuckyPatcher:
                    {
                        #region Структура

                        /*

                            appname
                                data.lpbkp
                                    данные
                                sddata.lpbkp
                                    данные

                        */

                        #endregion

                        string dataFile = Path.Combine(pathToBackup, "data.lpbkp");
                        string sddataFile = Path.Combine(pathToBackup, "sddata.lpbkp");

                        if (LFile.Exists(dataFile))
                            LFile.Move(dataFile, resultInternalDataPath);

                        if (LFile.Exists(sddataFile))
                            LFile.Move(sddataFile, resultExternalDataPath);

                        break;
                    }
                default:
                    throw new NotSupportedException($"`{backupType}` is not supported at the moment");
            }
        }

        public static void SplitObbFiles(
            [NotNull] IEnumerable<string> obbFilePaths,
            [NotNull] string partsFolderPath,
            IProgress<(long bytesWritten, long totalBytes)> progressNotifier
        )
        {
            Guard.NotNullArgument(obbFilePaths, nameof(obbFilePaths));
            Guard.NotNullArgument(partsFolderPath, nameof(partsFolderPath));

            LDirectory.Delete(partsFolderPath, true);
            LDirectory.CreateDirectory(partsFolderPath);

            string obbFilesDesc = Path.Combine(partsFolderPath, "paths.txt");

            string[] obbFiles = obbFilePaths.ToArray();

            long max = obbFiles.Sum(f => f.Length);
            long now = 0;

            const int bufferSize = 2048;
            const int fileSize = 40 * 1024 * 1024;

            int filesIndex = 1;

            using (var infoWriter = new StreamWriter(LFile.Create(obbFilesDesc), Encoding.ASCII))
            {
                byte[] buffer = new byte[bufferSize];

                foreach (string obbFile in obbFiles)
                {
                    infoWriter.WriteLine(Path.GetFileName(obbFile));
                    infoWriter.WriteLine(FileUtils.FileLength(obbFile));

                    int wrote = 0;
                    int index = filesIndex;

                    using (var input = LFile.OpenRead(obbFile))
                    {
                        FileStream output = LFile.Create(Path.Combine(partsFolderPath, index++ + ".png"));

                        int read;
                        while ((read = input.Read(buffer, 0, bufferSize)) > 0)
                        {
                            output.Write(buffer, 0, read);

                            wrote += read;
                            now += read;

                            progressNotifier?.Report((now, max));

                            if (read < bufferSize)
                                break;

                            if (wrote > fileSize)
                            {
                                output.Close();

                                output = LFile.Create(Path.Combine(partsFolderPath, index++ + ".png"));

                                wrote = 0;
                            }
                        }

                        output.Close();
                    }

                    infoWriter.WriteLine(index - filesIndex);

                    filesIndex = index;
                }
            }
        }

        public static void AddFileToZip(
            [NotNull] string zipPath,
            [NotNull] string filePath,
            [NotNull] string pathInZip,
            CompressionType newEntryCompression
        )
        {
            Guard.NotNullArgument(zipPath, nameof(zipPath));
            Guard.NotNullArgument(filePath, nameof(filePath));
            Guard.NotNullArgument(pathInZip, nameof(pathInZip));

            AddFilesToZip(zipPath, new []{filePath}, new []{pathInZip}, newEntryCompression);
        }

        public static void AddFilesToZip(
            [NotNull] string zipPath,
            [ItemNotNull] string[] filePaths,
            [ItemNotNull] string[] pathsInZip,
            CompressionType newEntryCompression
        )
        {
            Guard.NotNullArgument(zipPath, nameof(zipPath));
            Guard.NotNullArgument(filePaths, nameof(filePaths));
            Guard.NotNullArgument(pathsInZip, nameof(pathsInZip));

            if (filePaths.Length != pathsInZip.Length)
                throw new ArgumentException($"`{nameof(filePaths)}` length must be equal to `{nameof(pathsInZip)}` length");

            if (filePaths.Length == 0)
                return;

            // SharpZipLib
            using (var zip = new SharpZipFile(zipPath))
            {
                for (int i = 0; i < filePaths.Length; i++)
                {
                    string filePath = filePaths[i];
                    string pathInZip = pathsInZip[i];

                    if (filePath == null)
                        throw new ArgumentNullException($"one of `{filePaths}` entries is null");
                    if (pathInZip == null)
                        throw new ArgumentNullException($"one of `{pathsInZip}` entries is null");

                    IZipEntry entry = zip.GetEntry(pathInZip);

                    if (entry != null)
                        zip.ReplaceFile(entry, filePath);
                    else
                        zip.AddToArchive(filePath, pathInZip, newEntryCompression);
                }

                zip.Save();
            }
        }

        public static void ExtractTarByEntry(
            [NotNull] string tarFileName,
            [NotNull] string targetDir
        )
        {
            Guard.NotNullArgument(tarFileName, nameof(tarFileName));
            Guard.NotNullArgument(targetDir, nameof(targetDir));

            LDirectory.Delete(targetDir);

            using (var gzipInput = new GZipInputStream(LFile.OpenRead(tarFileName)))
            {
                var tarInput = new TarInputStream(gzipInput);

                char[] illegalChars = Path.GetInvalidFileNameChars();

                TarEntry tarEntry;
                while ((tarEntry = tarInput.GetNextEntry()) != null)
                {
                    if (tarEntry.IsDirectory)
                        continue;

                    // Converts the unix forward slashes in the filenames to windows backslashes
                    //

                    string name = tarEntry.Name.Replace('/', '\\');

                    if (string.IsNullOrEmpty(name))
                        continue;

                    name = name.Replace("\\", "justtempstringtoreplace");

                    if (illegalChars.Any(c => name.Contains(c)))
                    {
                        foreach (var c in illegalChars)
                            if (name.Contains(c))
                                name = name.Replace(c, '_');
                    }

                    name = name.Replace("justtempstringtoreplace", "\\");

                    // Remove any root e.g. '\' because a PathRooted filename defeats Path.Combine
                    if (Path.IsPathRooted(name))
                        name = name.Substring(Path.GetPathRoot(name).Length);

                    // Apply further name transformations here as necessary

                    string outName = Path.Combine(targetDir, name);

                    string directoryName = Path.GetDirectoryName(outName) ?? string.Empty;
                    LDirectory.CreateDirectory(directoryName);

                    using (var outStr = LFile.Create(outName))
                        tarInput.CopyEntryContents(outStr);

                    // Set the modification date/time. This approach seems to solve timezone issues.
                    DateTime myDt = DateTime.SpecifyKind(tarEntry.ModTime, DateTimeKind.Utc);
                    LFile.SetLastWriteTime(outName, myDt);
                }
            }
        }

        public static void CreateArchive(
            [NotNull] string folderPath,
            [NotNull] string resultZipPath
        )
        {
            Guard.NotNullArgument(folderPath, nameof(folderPath));
            Guard.NotNullArgument(resultZipPath, nameof(resultZipPath));

            LDirectory.CreateDirectory(Path.GetDirectoryName(resultZipPath));

            // DotNetZip
            //{
            //    using (var zf = new Ionic.Zip.ZipFile(outZipName, Encoding.UTF8))
            //    {
            //        var files = LDirectory.EnumerateFiles(folderName, "*", SearchOption.AllDirectories);
            //        foreach (var file in files)
            //            zf.AddFile(file, Path.GetDirectoryName(file.Substring(folderName.Length + 1)));
            //        zf.Save();
            //    }
            //}

            // SharpZipLib
            {
                using (var zip = ICSharpCode.SharpZipLib.Zip.ZipFile.Create(resultZipPath))
                {
                    var files = LDirectory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories);

                    zip.BeginUpdate();

                    foreach (var file in files)
                        zip.Add(file, file.Substring(folderPath.Length + 1));

                    zip.CommitUpdate();
                }
            }
        }
    }
}
