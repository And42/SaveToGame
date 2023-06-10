using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AndroidHelper.Logic;
using AndroidHelper.Logic.Interfaces;
using Interfaces.OrganisationItems;
using Interfaces.ViewModels;
using JetBrains.Annotations;
using MVVM_Tools.Code.Classes;
using MVVM_Tools.Code.Commands;
using MVVM_Tools.Code.Disposables;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Resources.Localizations;
using SaveToGameWpf.Windows;
using SharedData.Enums;
using ZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

namespace SaveToGameWpf.Logic.ViewModels
{
    public class InstallApkViewModel : BindableBase, IInstallApkViewModel
    {
        private static readonly Regex PackageRegex = new Regex(@"package=""(?<packageName>[^""]+)""");

        [NotNull] private readonly IAppSettings _settings;
        [NotNull] private readonly NotificationManager _notificationManager;
        [NotNull] private readonly TempUtils _tempUtils;
        [NotNull] private readonly GlobalVariables _globalVariables;
        [NotNull] private readonly Provider<IApktoolExtra> _apktoolProvider;
        [NotNull] private readonly Provider<AdbInstallWindow> _adbInstallWindowProvider;

        public IAppIconsStorage IconsStorage { get; }

        public IProperty<IVisualProgress> VisualProgress { get; } = new FieldProperty<IVisualProgress>();
        public IProperty<ITaskBarManager> TaskBarManager { get; } = new FieldProperty<ITaskBarManager>();

        public IProperty<string> WindowTitle { get; } = new FieldProperty<string>();
        public IProperty<bool> Working { get; } = new FieldProperty<bool>();

        public IProperty<string> Apk { get; } = new FieldProperty<string>();
        public IProperty<string> Save { get; } = new FieldProperty<string>();
        public IProperty<string> Data { get; } = new FieldProperty<string>();
        public IProperty<string[]> Obb { get; } = new FieldProperty<string[]>();

        public IReadonlyProperty<string> AppTitle { get; }
        public IProperty<string> LogText { get; } = new FieldProperty<string>();

        public IActionCommand ChooseApkCommand { get; }
        public IActionCommand ChooseSaveCommand { get; }
        public IActionCommand ChooseDataCommand { get; }
        public IActionCommand ChooseObbCommand { get; }
        public IActionCommand StartCommand { get; }

        public InstallApkViewModel(
            [NotNull] IAppSettings appSettings,
            [NotNull] NotificationManager notificationManager,
            [NotNull] TempUtils tempUtils,
            [NotNull] GlobalVariables globalVariables,
            [NotNull] Provider<IApktoolExtra> apktoolProvider,
            [NotNull] Provider<AdbInstallWindow> adbInstallWindowProvider
        )
        {
            _settings = appSettings;
            _notificationManager = notificationManager;
            _tempUtils = tempUtils;
            _globalVariables = globalVariables;
            _apktoolProvider = apktoolProvider;
            _adbInstallWindowProvider = adbInstallWindowProvider;

            AppTitle = new DelegatedProperty<string>(
                valueResolver: () => Path.GetFileNameWithoutExtension(Apk.Value) + " mod",
                valueApplier: null
            ).DependsOn(Apk).AsReadonly();

            string iconsFolder = Path.Combine(_globalVariables.PathToResources, "icons");

            BitmapSource GetImage(string name) =>
                File.ReadAllBytes(Path.Combine(iconsFolder, name)).ToBitmap().ToBitmapSource();

            IconsStorage = new AppIconsStorage
            {
                Icon_xxhdpi = { Value = GetImage("xxhdpi.png") },
                Icon_xhdpi = { Value = GetImage("xhdpi.png") },
                Icon_hdpi = { Value = GetImage("hdpi.png") },
                Icon_mdpi = { Value = GetImage("mdpi.png") }
            };

            // commands
            ChooseApkCommand = new ActionCommand(() =>
            {
                var (success, filePath) = PickerUtils.PickFile(filter: MainResources.AndroidFiles + @" (*.apk)|*.apk");

                if (success)
                    Apk.Value = filePath;
            }, () => !Working.Value).BindCanExecute(Working);
            ChooseSaveCommand = new ActionCommand(() =>
            {
                if (_settings.BackupType == BackupType.LuckyPatcher)
                {
                    var (success, folderPath) = PickerUtils.PickFolder();

                    if (success)
                        Save.Value = folderPath;
                }
                else
                {
                    var (success, filePath) = PickerUtils.PickFile(filter: MainResources.Archives + @" (*.tar.gz)|*.tar.gz");

                    if (success)
                        Save.Value = filePath;
                }
            }, () => !Working.Value).BindCanExecute(Working);
            ChooseDataCommand = new ActionCommand(() =>
            {
                var (success, filePath) = PickerUtils.PickFile(filter: MainResources.ZipArchives + @" (*.zip)|*.zip");

                if (success)
                    Data.Value = filePath;
            }, () => !Working.Value).BindCanExecute(Working);
            ChooseObbCommand = new ActionCommand(() =>
            {
                var (success, filePaths) = PickerUtils.PickFiles(filter: MainResources.CacheFiles + @" (*.obb)|*.obb");

                if (success)
                    Obb.Value = filePaths;
            }, () => !Working.Value).BindCanExecute(Working);
            StartCommand = new ActionCommand(StartCommand_Execute, () => !Working.Value).BindCanExecute(Working);
        }

        public void SetIcon(string imagePath, AndroidAppIcon iconType)
        {
            BitmapSource Resize(int size)
            {
                var img = new Bitmap(imagePath);

                if (img.Height != img.Width || img.Height != size)
                    img = img.Resize(size, size);

                return img.ToBitmapSource();
            }

            switch (iconType)
            {
                case AndroidAppIcon.xxhdpi:
                    IconsStorage.Icon_xxhdpi.Value = Resize(144);
                    break;
                case AndroidAppIcon.xhdpi:
                    IconsStorage.Icon_xhdpi.Value = Resize(96);
                    break;
                case AndroidAppIcon.hdpi:
                    IconsStorage.Icon_hdpi.Value = Resize(72);
                    break;
                case AndroidAppIcon.mdpi:
                    IconsStorage.Icon_mdpi.Value = Resize(48);
                    break;
            }
        }

        private async void StartCommand_Execute()
        {
            string apkFile = Apk.Value;

            if (string.IsNullOrEmpty(apkFile) || !File.Exists(apkFile))
                return;

            using (CreateWorking())
            {
                LogText.Value = string.Empty;

                try
                {
                    byte[] xxhdpi = IconsStorage.GetXxhdpiBytes();
                    byte[] xhdpi = IconsStorage.GetXhdpiBytes();
                    byte[] hdpi = IconsStorage.GetHdpiBytes();
                    byte[] mdpi = IconsStorage.GetMdpiBytes();

                    var uiDispatcher = Dispatcher.CurrentDispatcher;
                    var currentCulture = Thread.CurrentThread.CurrentUICulture;
                    await Task.Factory.StartNew(() =>
                    {
                        Thread.CurrentThread.CurrentCulture = currentCulture;
                        Thread.CurrentThread.CurrentUICulture = currentCulture;

                        ProcessAll(xxhdpi, xhdpi, hdpi, mdpi, uiDispatcher);
                    });
                }
                catch (Exception ex)
                {
#if DEBUG
                    Trace.WriteLine(ex.ToString());
#endif
                    Log($"{MainResources.ErrorUp}: {ex.Message}");

                    var visualProgress = VisualProgress.Value;
                    if (visualProgress != null)
                    {
                        visualProgress.SetLabelText(MainResources.AllDone);
                        visualProgress.HideIndeterminateLabel();
                        visualProgress.HideBar();
                    }
                    TaskBarManager.Value?.SetNoneState();
                }
            }
        }

        private void ProcessAll(byte[] xxhdpiBytes, byte[] xhdpiBytes, byte[] hdpiBytes, byte[] mdpiBytes, Dispatcher uiThreadDispatcher)
        {
            const string internalDataInApkName = "data.save";
            const string externalDataInApkName = "extdata.save";

            IVisualProgress visualProgress = VisualProgress.Value;
            ITaskBarManager taskBarManager = TaskBarManager.Value;

            string apkFile = Apk.Value;
            string saveFile = Save.Value;
            string androidDataFile = Data.Value;
            string[] androidObbFiles = (string[])Obb.Value?.Clone() ?? new string[0];
            string appTitle = AppTitle.Value;
            bool alternativeSigning = _settings.AlternativeSigning;
            BackupType backupType = _settings.BackupType;

            // initializing
            visualProgress?.SetBarIndeterminate();
            visualProgress?.ShowBar();
            visualProgress?.ShowIndeterminateLabel();
            taskBarManager?.SetProgress(0);
            taskBarManager?.SetUsualState();

            void SetStep(string step, int stepNumber)
            {
                WindowTitle.Value = step;
                Log(step);

                const int maxStep = 5;
                int percentage = (stepNumber - 1) * 100 / maxStep;

                visualProgress?.SetLabelText(step);
                taskBarManager?.SetProgress(percentage);
            }

            SetStep(MainResources.StepInitializing, 1);

            string resultFilePath = Path.Combine(
                Path.GetDirectoryName(apkFile) ?? string.Empty,
                Path.GetFileNameWithoutExtension(apkFile) + "_mod.apk"
            );

            IApktoolExtra apktool = _apktoolProvider.Get();
            IProcessDataHandler dataHandler = new ProcessDataCombinedHandler(Log);

            ITempFileProvider tempFileProvider = _tempUtils.CreateTempFileProvider();
            ITempFolderProvider tempFolderProvider = _tempUtils.CreateTempFolderProvider();

            using (var stgContainerExtracted = AndroidHelper.Logic.Utils.TempUtils.UseTempFolder(tempFolderProvider))
            {
                // extracting SaveToGame container app
                SetStep(MainResources.CopyingStgApk, 2);

                string containerZipPath = Path.Combine(_globalVariables.PathToResources, "apk.zip");
                using (var zip = new ZipFile(containerZipPath)
                {
                    Password = _globalVariables.AdditionalFilePassword
                })
                {
                    zip.ExtractAll(stgContainerExtracted.TempFolder);
                }

                SetStep(MainResources.AddingData, 3);

                // creating assets folder for data
                string stgContainerAssetsPath = Path.Combine(stgContainerExtracted.TempFolder, "assets");
                Directory.CreateDirectory(stgContainerAssetsPath);

                // adding backup
                if (!string.IsNullOrEmpty(saveFile))
                {
                    string internalDataPath = Path.Combine(stgContainerAssetsPath, internalDataInApkName);
                    string externalDataPath = Path.Combine(stgContainerAssetsPath, externalDataInApkName);

                    ApkModifer.ParseBackup(
                        pathToBackup: saveFile,
                        backupType: backupType,
                        resultInternalDataPath: internalDataPath,
                        resultExternalDataPath: externalDataPath,
                        tempFolderProvider: tempFolderProvider
                    );
                }

                // adding external data
                if (!string.IsNullOrEmpty(androidDataFile))
                {
                    File.Copy(
                        androidDataFile,
                        Path.Combine(stgContainerAssetsPath, externalDataInApkName)
                    );
                }

                // adding obb files
                if (androidObbFiles.Length != 0)
                {
                    using (var obbParts = AndroidHelper.Logic.Utils.TempUtils.UseTempFolder(tempFolderProvider))
                    {
                        ApkModifer.SplitObbFiles(
                            obbFilePaths: androidObbFiles,
                            partsFolderPath: obbParts.TempFolder,
                            // todo: add progress
                            progressNotifier: null
                        );

                        string assetsDir = Path.Combine(stgContainerExtracted.TempFolder, "assets", "111111222222333333");
                        Directory.CreateDirectory(assetsDir);

                        IEnumerable<string> filesToAdd = Directory.EnumerateFiles(obbParts.TempFolder);
                        foreach (var file in filesToAdd)
                        {
                            File.Copy(file, Path.Combine(assetsDir, Path.GetFileName(file)));
                        }
                    }
                }

                // adding resigned apk to container
                using (var sourceWithoutMetaInf = AndroidHelper.Logic.Utils.TempUtils.UseTempFile(tempFileProvider))
                using (var sourceZipaligned = AndroidHelper.Logic.Utils.TempUtils.UseTempFile(tempFileProvider))
                using (var sourceResigned = AndroidHelper.Logic.Utils.TempUtils.UseTempFile(tempFileProvider))
                {
                    File.Copy(apkFile, sourceWithoutMetaInf.TempFile, overwrite: true);

                    bool deleteMetaInf = !alternativeSigning;
                    if (deleteMetaInf)
                        apktool.RemoveMetaInf(sourceWithoutMetaInf.TempFile);
                    
                    apktool.ZipAlign(
                        sourceApkPath: sourceWithoutMetaInf.TempFile,
                        alignedApkPath: sourceZipaligned.TempFile,
                        dataHandler: dataHandler
                    );
                    
                    apktool.Sign(
                        sourceApkPath: sourceZipaligned.TempFile,
                        signedApkPath: sourceResigned.TempFile,
                        tempFileProvider: tempFileProvider,
                        dataHandler: dataHandler,
                        deleteMetaInf: false
                    );

                    File.Copy(
                        sourceFileName: sourceResigned.TempFile,
                        destFileName: Path.Combine(stgContainerAssetsPath, "install.bin"),
                        overwrite: false
                    );
                }

                // modifying AndroidManifest
                {
                    string pathToManifest = Path.Combine(stgContainerExtracted.TempFolder, "AndroidManifest.xml");

                    string sourcePackageName = null;
                    string sourceSharedUserId = null;
                    using (var sourceManifest = AndroidHelper.Logic.Utils.TempUtils.UseTempFile(tempFileProvider))
                    {
                        apktool.ExtractSimpleManifest(
                            apkPath: apkFile,
                            resultManifestPath: sourceManifest.TempFile,
                            tempFolderProvider: tempFolderProvider
                        );

                        string manifestText = File.ReadAllText(sourceManifest.TempFile, Encoding.UTF8);

                        Match packageNameMatch = PackageRegex.Match(manifestText);
                        if (packageNameMatch.Success)
                            sourcePackageName = packageNameMatch.Groups["packageName"].Value;

                        Match sharedUserIdMatch = Regex.Match(manifestText, @":sharedUserId=""(?<sharedUserId>[^""]+)""");
                        if (sharedUserIdMatch.Success)
                            sourceSharedUserId = sharedUserIdMatch.Groups["sharedUserId"].Value;
                    }

                    string text = 
                        File.ReadAllText(pathToManifest, Encoding.UTF8)
                            .Replace("package=\"com.programmingmachines.savetogameextractor\"", $"package=\"{sourcePackageName}\"")
                            .Replace("@string/app_name", appTitle);

                    if (!sourceSharedUserId.IsNullOrEmpty())
                        text = text.Replace("<manifest ", $"<manifest android:sharedUserId=\"{sourceSharedUserId}\" ");
                    
                    File.WriteAllText(pathToManifest, text);
                }

                // update container target sdk version to be able to install target apk on android <= 6
                {
                    int apkSdkVersion;
                    if (!apktool.TryGetTargetSdkVersion(apkPath: apkFile, out apkSdkVersion))
                        apkSdkVersion = apktool.GetSdkVersion(apkPath: apkFile);

                    if (apkSdkVersion <= 22)
                    {
                        string pathToApktoolYml = Path.Combine(stgContainerExtracted.TempFolder, "apktool.yml");
                        
                        string apktoolYmlText = File.ReadAllText(pathToApktoolYml, Encoding.UTF8);
                        string newApktoolYmlText = Regex.Replace(
                            input: apktoolYmlText,
                            pattern: @"targetSdkVersion: '(?<targetSdkVersion>\d+)'",
                            replacement: "targetSdkVersion: '22'"
                        );
                        File.WriteAllText(pathToApktoolYml, newApktoolYmlText, Encoding.UTF8);
                    }
                }
                
                // update container version code to match target apk version code to be able to install it as an update for older versions
                {
                    int versionCode = apktool.GetVersionCode(apkPath: apkFile);
    
                    string pathToApktoolYml = Path.Combine(stgContainerExtracted.TempFolder, "apktool.yml");
                        
                    string apktoolYmlText = File.ReadAllText(pathToApktoolYml, Encoding.UTF8);
                    string newApktoolYmlText = Regex.Replace(
                        input: apktoolYmlText,
                        pattern: @"versionCode: '(?<versionCode>\d+)'",
                        replacement: $"versionCode: '{versionCode}'"
                    );
                    File.WriteAllText(pathToApktoolYml, newApktoolYmlText, Encoding.UTF8);
                }

                // adding icons
                {
                    string resFolder = Path.Combine(stgContainerExtracted.TempFolder, "res");
                    string iconsFolder = Path.Combine(stgContainerExtracted.TempFolder, "res", "mipmap-");

                    foreach (string dir in Directory.EnumerateDirectories(resFolder, "mipmap-*"))
                        Directory.Delete(dir, recursive: true);

                    void WriteIcon(string folder, byte[] imageBytes)
                    {
                        string directory = $"{iconsFolder}{folder}";
                        Directory.CreateDirectory(directory);
                        File.WriteAllBytes(Path.Combine(directory, "ic_launcher.png"), imageBytes);
                    }

                    WriteIcon("xxhdpi", xxhdpiBytes);
                    WriteIcon("xhdpi", xhdpiBytes);
                    WriteIcon("hdpi", hdpiBytes);
                    WriteIcon("mdpi", mdpiBytes);
                }

                // compiling + aligning + signing
                using (var compiledContainer = AndroidHelper.Logic.Utils.TempUtils.UseTempFile(tempFileProvider))
                using (var zipalignedContainer = AndroidHelper.Logic.Utils.TempUtils.UseTempFile(tempFileProvider))
                {
                    // compiling
                    SetStep(MainResources.StepCompiling, 4);

                    // todo: check errors
                    List<Error> compilationErrors;
                    apktool.Compile(
                        projectFolderPath: stgContainerExtracted.TempFolder,
                        destinationApkPath: compiledContainer.TempFile,
                        dataHandler: dataHandler,
                        errors: out compilationErrors
                    );

                    if (compilationErrors.Count > 0)
                    {
                        Log(MainResources.ErrorUp);
                        return;
                    }

                    bool deleteMetaInf = !alternativeSigning;
                    if (deleteMetaInf)
                        apktool.RemoveMetaInf(fileName: compiledContainer.TempFile);

                    // zipaligning
                    apktool.ZipAlign(
                        sourceApkPath: compiledContainer.TempFile,
                        alignedApkPath: zipalignedContainer.TempFile,
                        dataHandler: dataHandler
                    );
                    
                    // signing
                    SetStep(MainResources.StepSigning, 5);

                    apktool.Sign(
                        sourceApkPath: zipalignedContainer.TempFile,
                        signedApkPath: resultFilePath,
                        tempFileProvider: tempFileProvider,
                        dataHandler: dataHandler,
                        deleteMetaInf: false
                    );
                }
            }

            visualProgress?.HideIndeterminateLabel();
            visualProgress?.HideBar();
            SetStep(MainResources.AllDone, 6);
            Log(string.Empty);
            Log($"{MainResources.Path_to_file} {resultFilePath}");

            _globalVariables.LatestModdedApkPath = resultFilePath;

            if (_settings.Notifications)
            {
                _notificationManager.Show(
                    title: MainResources.Information_Title,
                    text: MainResources.ModificationCompletedContent
                );
            }

            string dialogResult = MessBox.ShowDial(
                $"{MainResources.Path_to_file} {resultFilePath}",
                MainResources.Successful,
                MainResources.OK, MainResources.Open, MainResources.Install
            );

            if (dialogResult == MainResources.Open)
            {
                Process.Start("explorer.exe", $"/select,{resultFilePath}");
            }
            else if (dialogResult == MainResources.Install)
            {
                uiThreadDispatcher.Invoke(() => _adbInstallWindowProvider.Get().ShowDialog());
            }

            taskBarManager?.SetNoneState();
        }

        private void Log(string text)
        {
            LogText.Value += text + "\n";
        }

        private CustomBoolDisposable CreateWorking()
        {
            return new CustomBoolDisposable(val => Working.Value = val);
        }
    }
}
