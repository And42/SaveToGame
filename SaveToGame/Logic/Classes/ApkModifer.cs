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
using JetBrains.Annotations;
using LongPaths.Logic;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Resources;
using TempUtils = AndroidHelper.Logic.Utils.TempUtils;

namespace SaveToGameWpf.Logic.Classes
{
    /// <summary>
    /// Класс для изменения апк файлов
    /// </summary>
    public class ApkModifer
    {
        /// <summary>
        /// Вызывается при сохранении файлов в архив
        /// </summary>
        public event Action<(long current, long maximum)> ProgressChanged;

        [NotNull] private readonly object _lock = new object();

        [NotNull] private readonly IApktool _apktool;
        [NotNull] private readonly string _apkPath;
        [NotNull] private readonly ITempFolderProvider _tempFolderProvider;

        [CanBeNull] private (byte[] iv, byte[] key)? _encryptionInfo;
        [CanBeNull] private (string pathToBackup, BackupType backupType)? _backupInfo;
        [CanBeNull] private (string pathToDataZip, bool? _)? _externalDataInfo;
        [CanBeNull] private (string[] pathsToObbs, bool? _)? _obbInfo;
        [CanBeNull] private (string message, int messagesCount)? _messageInfo;

        /// <summary>
        /// Создаёт экземпляр класса ApkModifer на основании apktools
        /// </summary>
        /// <param name="apktool">Apktools</param>
        public ApkModifer(
            [NotNull] IApktool apktool,
            [NotNull] string apkPath,
            [NotNull] ITempFolderProvider tempFolderProvider
        )
        {
            Guard.NotNullArgument(apktool, nameof(apktool));
            Guard.NotNullArgument(apkPath, nameof(apkPath));
            Guard.NotNullArgument(tempFolderProvider, nameof(tempFolderProvider));

            _apktool = apktool;
            _apkPath = apkPath;
            _tempFolderProvider = tempFolderProvider;
        }

        [NotNull]
        public ApkModifer Encrypt(
            [NotNull] byte[] iv,
            [NotNull] byte[] key
        )
        {
            Guard.NotNullArgument(iv, nameof(iv));
            Guard.NotNullArgument(key, nameof(key));

            lock (_lock)
            {
                _encryptionInfo = (iv: iv, key: key);
            }

            return this;
        }

        /// <summary>
        /// Добавляет сохранение в декомпилированный файл
        /// </summary>
        /// <param name="pathToBackup"></param>
        /// <param name="key">AES Key</param>
        /// <param name="backupType"></param>
        /// <param name="iv">AES IV</param>
        [NotNull]
        public ApkModifer Backup(
            [NotNull] string pathToBackup,
            BackupType backupType
        )
        {
            Guard.NotNullArgument(pathToBackup, nameof(pathToBackup));

            if (!Enum.IsDefined(typeof(BackupType), backupType))
                throw new ArgumentException($@"`{backupType}` is not a valid enum member", nameof(backupType));

            lock (_lock)
            {
                _backupInfo = (pathToBackup: pathToBackup, backupType: backupType);
            }

            return this;
        }

        /// <summary>
        /// Добавляет архив с данными Android/data в декомпилированное приложение
        /// </summary>
        /// <param name="zipName"></param>
        [NotNull]
        public ApkModifer ExternalData([NotNull] string zipName)
        {
            Guard.NotNullArgument(zipName, nameof(zipName));

            lock (_lock)
            {
                _externalDataInfo = (pathToDataZip: zipName, _: null);
            }

            return this;
        }

        /// <summary>
        /// Добавляет файл obb в декомпилированный файл
        /// </summary>
        /// <param name="files">Файлы для добавления</param>
        [NotNull]
        public ApkModifer ExternalObb([NotNull] params string[] files)
        {
            Guard.NotNullArgument(files, nameof(files));

            lock (_lock)
            {
                _obbInfo = (pathsToObbs: files, _: null);
            }

            return this;
        }

        [NotNull]
        public ApkModifer Message(
            [NotNull] string message,
            int messagesCount
        )
        {
            Guard.NotNullArgument(message, nameof(message));

            if (messagesCount < 0)
                throw new ArgumentOutOfRangeException(nameof(messagesCount));

            lock (_lock)
            {
                _messageInfo = (message: message, messagesCount: messagesCount);
            }

            return this;
        }

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

        public void Process()
        {
            const string internalDataName = "data.save";
            const string externalDataName = "extdata.save";
            const string obbName = "extobb.save";

            lock (_lock)
            {
                // adding backup
                if (_backupInfo != null)
                {
                    (string pathToBackup, BackupType backupType) = _backupInfo.Value;

                    using (var convertedBackups = TempUtils.UseTempFolder(_tempFolderProvider))
                    {
                        ParseBackup(
                            pathToBackup,
                            backupType,
                            Path.Combine(convertedBackups.TempFolder, internalDataName),
                            Path.Combine(convertedBackups.TempFolder, externalDataName),
                            //Path.Combine(convertedBackups.TempFolder, obbName),
                            _tempFolderProvider
                        );

                        string[] filesToProcess = {internalDataName, externalDataName, obbName};

                        foreach (string fileToProcess in filesToProcess)
                        {
                            string fullPath = Path.Combine(convertedBackups.TempFolder, fileToProcess);

                            if (!LFile.Exists(fullPath))
                                continue;

                            string fileToAdd = fullPath;

                            if (_encryptionInfo != null && fileToProcess == internalDataName)
                            {
                                (byte[] iv, byte[] key) = _encryptionInfo.Value;

                                fileToAdd = fullPath + ".enc";
                                CommonUtils.EncryptFile(
                                    filePath: fullPath,
                                    outputPath: fileToAdd,
                                    iv: iv,
                                    key: key
                                );
                            }

                            AddFileToZip(_apkPath, fileToAdd, "assets/" + fileToProcess, CompressionType.Store);
                        }
                    }
                }

                // adding external data (replaces backed up external data if previously added)
                if (_externalDataInfo != null)
                {
                    (string pathToDataZip, bool? _) = _externalDataInfo.Value;

                    AddFileToZip(_apkPath, pathToDataZip, "assets/" + externalDataName, CompressionType.Store);
                }

                // adding obb files
                if (_obbInfo != null)
                {
                    (string[] pathsToObbs, bool? _) = _obbInfo.Value;

                    // result path: assets/111111222222333333/...
                    using (var tempFiles = TempUtils.UseTempFolder(_tempFolderProvider))
                    {
                        var p = ProgressChanged;
                        
                        SplitObbFiles(
                            obbFilePaths: pathsToObbs,
                            partsFolderPath: tempFiles.TempFolder,
                            progressNotifier: p != null ? new Progress<(long bytesWritten, long totalBytes)>(p.Invoke) : null 
                        );

                        string[] filesToAdd = LDirectory.GetFiles(tempFiles.TempFolder);

                        AddFilesToZip(
                            _apkPath,
                            filesToAdd,
                            Array.ConvertAll(filesToAdd, it => "assets/111111222222333333/" + Path.GetFileName(it)),
                            CompressionType.Store
                        );
                    }
                }

                // adding smali file
                bool addBackupRestoreCode = _backupInfo != null || _externalDataInfo != null || _obbInfo != null;
                bool addMessageCode = _messageInfo != null;

                if (addBackupRestoreCode || addMessageCode)
                {
                    if (_encryptionInfo == null)
                        throw new InvalidOperationException("you have to use encryption for processing");

                    using (var decompiledFolder = TempUtils.UseTempFolder(_tempFolderProvider))
                    {
                        _apktool.Baksmali(_apkPath, decompiledFolder.TempFolder, _tempFolderProvider, null);

                        string manifestPath = Path.Combine(decompiledFolder.TempFolder, "AndroidManifest.xml");

                        _apktool.ExtractSimpleManifest(_apkPath, manifestPath, _tempFolderProvider);

                        var manifest = new AndroidManifest(manifestPath);

                        if (manifest.MainSmaliFile == null)
                            throw new Exception($"`{nameof(manifest.MainSmaliFile)}` is null");

                        string smaliDir = manifest.MainSmaliPath.Substring(decompiledFolder.TempFolder.Length + 1);
                        smaliDir = smaliDir.Substring(0, smaliDir.IndexOf(Path.DirectorySeparatorChar));

                        string saveGameDir = Path.Combine(decompiledFolder.TempFolder, smaliDir, "com", "savegame");

                        LDirectory.CreateDirectory(saveGameDir);

                        (byte[] iv, byte[] key) = _encryptionInfo.Value;
                        
                        CommonUtils.GenerateAndSaveSmali(
                            filePath: Path.Combine(saveGameDir, "SavesRestoringPortable.smali"),
                            iv: iv,
                            key: key,
                            addSave: addBackupRestoreCode,
                            message: addMessageCode ? _messageInfo.Value.message : string.Empty,
                            messagesCount: addMessageCode ? _messageInfo.Value.messagesCount : 0
                        );

                        manifest.MainSmaliFile.AddTextToMethod(FileResources.MainSmaliCall);
                        manifest.MainSmaliFile.Save();

                        LFile.Delete(manifestPath);

                        using (var folderWithDexes = TempUtils.UseTempFolder(_tempFolderProvider))
                        {
                            _apktool.Smali(decompiledFolder.TempFolder, folderWithDexes.TempFolder, null);

                            string[] dexFiles = LDirectory.GetFiles(folderWithDexes.TempFolder);

                            AddFilesToZip(
                                _apkPath,
                                dexFiles,
                                Array.ConvertAll(dexFiles, Path.GetFileName),
                                CompressionType.Store
                            );
                        }
                    }
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
