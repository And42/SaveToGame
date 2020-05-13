using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AndroidHelper.Logic;
using AndroidHelper.Logic.Interfaces;
using ICSharpCode.SharpZipLib.Zip;
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

namespace SaveToGameWpf.Logic.ViewModels
{
    public class InstallApkViewModel : BindableBase, IInstallApkViewModel
    {
        private static readonly Regex PackageRegex = new Regex(@"package=""(?<packageName>[^""]+)""");

        [NotNull] private readonly IAppSettings _settings;
        [NotNull] private readonly NotificationManager _notificationManager;
        [NotNull] private readonly TempUtils _tempUtils;
        [NotNull] private readonly GlobalVariables _globalVariables;
        [NotNull] private readonly Provider<IApktool> _apktoolProvider;
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
            [NotNull] Provider<IApktool> apktoolProvider,
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

            IApktool apktool = _apktoolProvider.Get();
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
                using (var sourceResigned = AndroidHelper.Logic.Utils.TempUtils.UseTempFile(tempFileProvider))
                {
                    apktool.Sign(
                        sourceApkPath: apkFile,
                        signedApkPath: sourceResigned.TempFile,
                        tempFileProvider: tempFileProvider,
                        dataHandler: dataHandler,
                        deleteMetaInf: !alternativeSigning
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
                    }

                    File.WriteAllText(
                        pathToManifest,
                        File.ReadAllText(pathToManifest, Encoding.UTF8)
                            .Replace("change_package", sourcePackageName)
                            .Replace("@string/app_name", appTitle)
                    );
                }

                // adding icons
                {
                    string iconsFolder = Path.Combine(stgContainerExtracted.TempFolder, "res", "mipmap-");

                    void DeleteIcon(string folder) =>
                        File.Delete(Path.Combine($"{iconsFolder}{folder}", "ic_launcher.png"));

                    DeleteIcon("xxhdpi-v4");
                    DeleteIcon("xhdpi-v4");
                    DeleteIcon("hdpi-v4");
                    DeleteIcon("mdpi-v4");

                    void WriteIcon(string folder, byte[] imageBytes) =>
                        File.WriteAllBytes(Path.Combine($"{iconsFolder}{folder}", "ic_launcher.png"), imageBytes);

                    WriteIcon("xxhdpi-v4", xxhdpiBytes);
                    WriteIcon("xhdpi-v4", xhdpiBytes);
                    WriteIcon("hdpi-v4", hdpiBytes);
                    WriteIcon("mdpi-v4", mdpiBytes);
                }

                // compiling + signing
                using (var compiledContainer = AndroidHelper.Logic.Utils.TempUtils.UseTempFile(tempFileProvider))
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

                    // signing
                    SetStep(MainResources.StepSigning, 5);

                    apktool.Sign(
                        sourceApkPath: compiledContainer.TempFile,
                        signedApkPath: resultFilePath,
                        tempFileProvider: tempFileProvider,
                        dataHandler: dataHandler,
                        deleteMetaInf: !alternativeSigning
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
