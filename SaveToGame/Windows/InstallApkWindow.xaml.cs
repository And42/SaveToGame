using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using AndroidHelper.Logic;
using AndroidHelper.Logic.Interfaces;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using LongPaths.Logic;
using Microsoft.Win32;
using MVVM_Tools.Code.Disposables;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Resources.Localizations;
using BackupType = SaveToGameWpf.Logic.Classes.BackupType;
using DragEventArgs = System.Windows.DragEventArgs;
using ATempUtils = AndroidHelper.Logic.Utils.TempUtils;

namespace SaveToGameWpf.Windows
{
    public partial class InstallApkWindow
    {
        public AppIconsStorage IconsStorage { get; }

        public Property<string> WindowTitle { get; } = new Property<string>();
        public Property<bool> Working { get; } = new Property<bool>();

        public Property<string> Apk { get; } = new Property<string>();
        public Property<string> Save { get; } = new Property<string>();
        public Property<string> Data { get; } = new Property<string>();
        public Property<string[]> Obb { get; } = new Property<string[]>();

        public Property<string> AppTitle { get; } = new Property<string>();

        [NotNull] private readonly AppSettings _settings;
        [NotNull] private readonly NotificationManager _notificationManager;
        [NotNull] private readonly TempUtils _tempUtils;
        [NotNull] private readonly GlobalVariables _globalVariables;
        [NotNull] private readonly Provider<IApktool> _apktoolProvider;

        private readonly StringBuilder _log = new StringBuilder();
        private readonly IVisualProgress _visualProgress;
        private readonly ITaskBarManager _taskBarManager;

        public InstallApkWindow(
            [NotNull] AppSettings appSettings,
            [NotNull] NotificationManager notificationManager,
            [NotNull] TempUtils tempUtils,
            [NotNull] GlobalVariables globalVariables,
            [NotNull] Provider<IApktool> apktoolProvider
        )
        {
            _settings = appSettings;
            _notificationManager = notificationManager;
            _tempUtils = tempUtils;
            _globalVariables = globalVariables;
            _apktoolProvider = apktoolProvider;

            string iconsFolder = Path.Combine(_globalVariables.PathToResources, "icons");

            BitmapSource GetImage(string name) =>
                LFile.ReadAllBytes(Path.Combine(iconsFolder, name)).ToBitmap().ToBitmapSource();

            IconsStorage = new AppIconsStorage
            {
                Icon_xxhdpi = { Value = GetImage("xxhdpi.png") },
                Icon_xhdpi = { Value = GetImage("xhdpi.png") },
                Icon_hdpi = { Value = GetImage("hdpi.png") },
                Icon_mdpi = { Value = GetImage("mdpi.png") }
            };

            InitializeComponent();

            _taskBarManager = new TaskBarManager(TaskbarItemInfo = new TaskbarItemInfo());
            _visualProgress = StatusProgress.GetVisualProgress();

            _visualProgress.SetLabelText(MainResources.AllDone);

            Apk.PropertyChanged += (sender, args) => AppTitle.Value = Path.GetFileNameWithoutExtension(Apk.Value) + " mod";
        }

        #region Buttons

        private void ChooseApkBtn_Click(object sender, EventArgs e)
        {
            var (success, filePath) = PickerUtils.PickFile(filter: MainResources.AndroidFiles + @" (*.apk)|*.apk");

            if (success)
                Apk.Value = filePath;
        }

        private void ChooseSaveBtn_Click(object sender, EventArgs e)
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
        }

        private void ChooseDataClick(object sender, RoutedEventArgs e)
        {
            var (success, filePath) = PickerUtils.PickFile(filter: MainResources.ZipArchives + @" (*.zip)|*.zip");

            if (success)
                Data.Value = filePath;
        }

        private void ChooseObbClick(object sender, RoutedEventArgs e)
        {
            var (success, filePaths) = PickerUtils.PickFiles(filter: MainResources.CacheFiles + @" (*.obb)|*.obb");

            if (success)
                Obb.Value = filePaths;
        }

        private async void StartClick(object sender, RoutedEventArgs e)
        {
            string apkFile = Apk.Value;

            if (string.IsNullOrEmpty(apkFile) || !LFile.Exists(apkFile))
                return;

            using (CreateWorking())
            {
                _log.Clear();
                LogBox.Text = "";

                try
                {
                    byte[] xxhdpi = IconsStorage.GetXxhdpiBytes();
                    byte[] xhdpi = IconsStorage.GetXhdpiBytes();
                    byte[] hdpi = IconsStorage.GetHdpiBytes();
                    byte[] mdpi = IconsStorage.GetMdpiBytes();

                    var currentCulture = Thread.CurrentThread.CurrentUICulture;
                    await Task.Factory.StartNew(() =>
                    {
                        Thread.CurrentThread.CurrentCulture = currentCulture;
                        Thread.CurrentThread.CurrentUICulture = currentCulture;

                        ProcessAll(xxhdpi, xhdpi, hdpi, mdpi);
                    });
                }
                catch (Exception ex)
                {
#if DEBUG
                    Trace.WriteLine(ex.ToString());
                    Dispatcher.Invoke(() => throw ex);
#endif
                    Log($"{MainResources.ErrorUp}: {ex.Message}");

                    _visualProgress.SetLabelText(MainResources.AllDone);
                    _visualProgress.HideIndeterminateLabel();
                    _visualProgress.HideBar();
                    _taskBarManager.SetNoneState();
                }
            }
        }

        #endregion

        #region Drag & Drop

        private void Apk_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".apk");
        }

        private void Apk_DragDrop(object sender, DragEventArgs e)
        {
            e.DropOneByEnd(".apk", file => Apk.Value = file);
        }

        private void Save_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".tar.gz");
        }

        private void Save_DragDrop(object sender, DragEventArgs e)
        {
            e.DropOneByEnd(".tar.gz", file => Save.Value = file);
        }

        private void Data_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".zip");
        }

        private void Data_DragDrop(object sender, DragEventArgs e)
        {
            e.DropOneByEnd(".zip", file => Data.Value = file);
        }

        private void Obb_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".obb");
        }

        private void Obb_DragDrop(object sender, DragEventArgs e)
        {
            e.DropManyByEnd(".obb", files => Obb.Value = files);
        }

        private void Icon_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".png");
        }

        private void Icon_Drop(object sender, DragEventArgs e)
        {
            e.DropOneByEnd(".png", file =>
            {
                string tag = sender.As<FrameworkElement>().Tag.As<string>();
                SetIcon(file, tag);
            });
        }

        #endregion

        private void ProcessAll(byte[] xxhdpiBytes, byte[] xhdpiBytes, byte[] hdpiBytes, byte[] mdpiBytes)
        {
            const string internalDataInApkName = "data.save";
            const string externalDataInApkName = "extdata.save";

            string apkFile = Apk.Value;
            string saveFile = Save.Value;
            string androidDataFile = Data.Value;
            string[] androidObbFiles = (string[]) Obb.Value?.Clone() ?? new string[0];
            string appTitle = AppTitle.Value;
            bool alternativeSigning = _settings.AlternativeSigning;
            BackupType backupType = _settings.BackupType;

            // initializing
            _visualProgress.SetBarIndeterminate();
            _visualProgress.ShowBar();
            _visualProgress.ShowIndeterminateLabel();
            _taskBarManager.SetProgress(0);
            _taskBarManager.SetUsualState();

            void SetStep(string step, int stepNumber)
            {
                WindowTitle.Value = step;
                Log(step);

                const int maxStep = 5;
                int percentage = (stepNumber - 1) * 100 / maxStep;

                _visualProgress.SetLabelText(step);
                _taskBarManager.SetProgress(percentage);
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

            using (var stgContainerExtracted = ATempUtils.UseTempFolder(tempFolderProvider))
            {
                // extracting SaveToGame container app
                SetStep(MainResources.CopyingStgApk, 2);

                string containerZipPath = Path.Combine(_globalVariables.PathToResources, "apk.zip");
                using (var zip = new ZipFile(containerZipPath)
                {
                    Password = GlobalVariables.AdditionalFilePassword
                })
                {
                    zip.ExtractAll(stgContainerExtracted.TempFolder);
                }

                SetStep(MainResources.AddingData, 3);

                // adding backup
                if (!string.IsNullOrEmpty(saveFile))
                {
                    string internalDataPath = Path.Combine(stgContainerExtracted.TempFolder, "assets", internalDataInApkName);
                    string externalDataPath = Path.Combine(stgContainerExtracted.TempFolder, "assets", externalDataInApkName);

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
                    LFile.Copy(
                        androidDataFile,
                        Path.Combine(stgContainerExtracted.TempFolder, "assets", externalDataInApkName)
                    );
                }

                // adding obb files
                if (androidObbFiles.Length != 0)
                {
                    using (var obbParts = ATempUtils.UseTempFolder(tempFolderProvider))
                    {
                        ApkModifer.SplitObbFiles(
                            obbFilePaths: androidObbFiles,
                            partsFolderPath: obbParts.TempFolder,
                            // todo: add progress
                            progressNotifier: null
                        );

                        IEnumerable<string> filesToAdd = LDirectory.EnumerateFiles(obbParts.TempFolder);

                        foreach (var file in filesToAdd)
                        {
                            LFile.Copy(
                                file,
                                Path.Combine(
                                    stgContainerExtracted.TempFolder,
                                    "assets", "111111222222333333",
                                    Path.GetFileName(file)
                                )
                            );
                        }
                    }
                }

                // adding resigned apk to container
                using (var sourceResigned = ATempUtils.UseTempFile(tempFileProvider))
                {
                    apktool.Sign(
                        sourceApkPath: apkFile,
                        signedApkPath: sourceResigned.TempFile,
                        tempFileProvider: tempFileProvider,
                        dataHandler: dataHandler,
                        deleteMetaInf: !alternativeSigning
                    );

                    LFile.Copy(
                        sourceFileName: sourceResigned.TempFile,
                        destFileName: Path.Combine(stgContainerExtracted.TempFolder, "assets", "install.bin"),
                        overwrite: false
                    );
                }

                // modifying AndroidManifest
                {
                    string pathToManifest = Path.Combine(stgContainerExtracted.TempFolder, "AndroidManifest.xml");

                    string sourcePackageName;
                    using (var sourceManifest = ATempUtils.UseTempFile(tempFileProvider))
                    {
                        apktool.ExtractSimpleManifest(
                            apkPath: apkFile,
                            resultManifestPath: sourceManifest.TempFile,
                            tempFolderProvider: tempFolderProvider
                        );

                        sourcePackageName = new AndroidManifest(sourceManifest.TempFile).Package;
                    }

                    LFile.WriteAllText(
                        pathToManifest,
                        LFile.ReadAllText(pathToManifest, Encoding.UTF8)
                            .Replace("change_package", sourcePackageName)
                            .Replace("@string/app_name", appTitle)
                    );
                }

                // adding icons
                {
                    string iconsFolder = Path.Combine(stgContainerExtracted.TempFolder, "res", "mipmap-");

                    void DeleteIcon(string folder) =>
                        LFile.Delete(Path.Combine($"{iconsFolder}{folder}", "ic_launcher.png"));

                    DeleteIcon("xxhdpi-v4");
                    DeleteIcon("xhdpi-v4");
                    DeleteIcon("hdpi-v4");
                    DeleteIcon("mdpi-v4");

                    void WriteIcon(string folder, byte[] imageBytes) =>
                        LFile.WriteAllBytes(Path.Combine($"{iconsFolder}{folder}", "ic_launcher.png"), imageBytes);

                    WriteIcon("xxhdpi-v4", xxhdpiBytes);
                    WriteIcon("xhdpi-v4", xhdpiBytes);
                    WriteIcon("hdpi-v4", hdpiBytes);
                    WriteIcon("mdpi-v4", mdpiBytes);
                }

                // compiling + signing
                using (var compiledContainer = ATempUtils.UseTempFile(tempFileProvider))
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

            _visualProgress.HideIndeterminateLabel();
            _visualProgress.HideBar();
            SetStep(MainResources.AllDone, 6);

            if (_settings.Notifications)
            {
                _notificationManager.Show(
                    title: MainResources.Information_Title,
                    text: MainResources.ModificationCompletedContent
                );
            }

            if (MessBox.ShowDial(
                    MainResources.Path_to_file + resultFilePath,
                    MainResources.Successful,
                    MainResources.OK, MainResources.Open
                ) == MainResources.Open)
            {
                Process.Start("explorer.exe", $"/select,{resultFilePath}");
            }

            _taskBarManager.SetNoneState();
        }

        private void SetIcon(string imagePath, string tag)
        {
            BitmapSource Resize(int size)
            {
                var img = new Bitmap(imagePath);

                if (img.Height != img.Width || img.Height != size)
                    img = img.Resize(size, size);

                return img.ToBitmapSource();
            }

            switch (tag)
            {
                case "xxhdpi":
                    IconsStorage.Icon_xxhdpi.Value = Resize(144);
                    break;
                case "xhdpi":
                    IconsStorage.Icon_xhdpi.Value = Resize(96);
                    break;
                case "hdpi":
                    IconsStorage.Icon_hdpi.Value = Resize(72);
                    break;
                case "mdpi":
                    IconsStorage.Icon_mdpi.Value = Resize(48);
                    break;
            }
        }

        private void ChooseImage_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = MainResources.Images + @" (*.png)|*.png"
            };

            if (dialog.ShowDialog() == true)
                SetIcon(dialog.FileName, sender.As<FrameworkElement>().Tag.As<string>());
        }

        private void Log(string text)
        {
            _log.Append(text);
            _log.Append('\n');
            Dispatcher.Invoke(() =>
            {
                LogBox.Text = _log.ToString();
                LogBox.ScrollToEnd();
            });
        }

        #region Disposables

        private CustomBoolDisposable CreateWorking()
        {
            return new CustomBoolDisposable(val => Working.Value = val);
        }

        #endregion
    }
}
