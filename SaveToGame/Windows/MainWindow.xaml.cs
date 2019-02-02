using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using AndroidHelper.Logic;
using AndroidHelper.Logic.Interfaces;
using JetBrains.Annotations;
using LongPaths.Logic;
using MVVM_Tools.Code.Disposables;
using SaveToGameWpf.Logic;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Logic.ViewModels;
using SaveToGameWpf.Resources;
using SaveToGameWpf.Resources.Localizations;

using Application = System.Windows.Application;
using BackupType = SaveToGameWpf.Logic.Classes.BackupType;
using DragEventArgs = System.Windows.DragEventArgs;
using ATempUtils = AndroidHelper.Logic.Utils.TempUtils;

namespace SaveToGameWpf.Windows
{
    public sealed partial class MainWindow
    {
        // how many times app should try to create log file for the apk file processing
        private const int LogCreationTries = 50;

        private static readonly string Line = new string('-', 50);

        [NotNull] private readonly AppSettings _settings;
        [NotNull] private readonly ApplicationUtils _applicationUtils;
        [NotNull] private readonly Provider<MainWindow> _mainWindowProvider;
        [NotNull] private readonly Provider<InstallApkWindow> _installApkWindowProvider;
        [NotNull] private readonly Provider<AboutWindow> _aboutWindowProvider;
        [NotNull] private readonly NotificationManager _notificationManager;
        [NotNull] private readonly TempUtils _tempUtils;
        [NotNull] private readonly GlobalVariables _globalVariables;
        [NotNull] private readonly Utils _utils;
        [NotNull] private readonly Provider<IApktool> _apktoolProvider;

        private readonly IVisualProgress _visualProgress;
        private readonly ITaskBarManager _taskBarManager;

        public MainWindowViewModel ViewModel { get; }

        private StreamWriter _currentLog;

        private bool _shutdownOnClose = true;

        public MainWindow(
            [NotNull] AppSettings appSettings,
            [NotNull] ApplicationUtils applicationUtils,
            [NotNull] MainWindowViewModel viewModel,
            [NotNull] Provider<MainWindow> mainWindowProvider,
            [NotNull] Provider<InstallApkWindow> installApkWindowProvider,
            [NotNull] Provider<AboutWindow> aboutWindowProvider,
            [NotNull] NotificationManager notificationManager,
            [NotNull] TempUtils tempUtils,
            [NotNull] GlobalVariables globalVariables,
            [NotNull] Utils utils,
            [NotNull] Provider<IApktool> apktoolProvider
        )
        {
            _settings = appSettings;
            _applicationUtils = applicationUtils;
            _mainWindowProvider = mainWindowProvider;
            _installApkWindowProvider = installApkWindowProvider;
            _aboutWindowProvider = aboutWindowProvider;
            _notificationManager = notificationManager;
            _tempUtils = tempUtils;
            _globalVariables = globalVariables;
            _utils = utils;
            _apktoolProvider = apktoolProvider;

            ViewModel = viewModel;
		    DataContext = ViewModel;

            InitializeComponent();

		    _taskBarManager = new TaskBarManager(TaskbarItemInfo = new TaskbarItemInfo());

            _visualProgress = StatusProgress.GetVisualProgress();

            _visualProgress.SetLabelText(MainResources.AllDone);
        }

        #region Window events

        private async void MainWindow_Loaded(object sender, EventArgs e)
        {
            await CheckJavaVersion();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            _settings.PopupMessage = ViewModel.PopupBoxText.Value;

            if (_shutdownOnClose)
            {
                Application.Current.Shutdown();
            }
        }

        #endregion

        #region Button click handlers

        private void ChooseApkBtn_Click(object sender, EventArgs e)
        {
            var (success, filePath) = PickerUtils.PickFile(filter: MainResources.AndroidFiles + @" (*.apk)|*.apk");

            if (!success)
                return;

            ViewModel.CurrentApk.Value = filePath;
            ChooseApkButton.ToolTip = filePath;
        }

        private void ChooseSaveBtn_Click(object sender, EventArgs e)
        {
            if (ViewModel.BackupType != BackupType.LuckyPatcher)
            {
                var (success, filePath) = PickerUtils.PickFile(filter: MainResources.Archives + @" (*.tar.gz)|*.tar.gz");

                if (success)
                    ViewModel.CurrentSave.Value = filePath;
            }
            else
            {
                var (success, folderPath) = PickerUtils.PickFolder();

                if (success)
                    ViewModel.CurrentSave.Value = folderPath;
            }
        }

        private void StartBtn_Click(object sender, EventArgs e)
        {
            string apkFile = ViewModel.CurrentApk.Value;
            string saveFile = ViewModel.CurrentSave.Value;

            #region Проверка на существование файлов

            if (string.IsNullOrEmpty(apkFile) || !LFile.Exists(apkFile) ||
                (ViewModel.SavePlusMess.Value || ViewModel.OnlySave.Value) &&
                (string.IsNullOrEmpty(saveFile) || !LFile.Exists(saveFile) && !LDirectory.Exists(saveFile))
            )
            {
                HaveError(MainResources.File_or_save_not_selected, MainResources.File_or_save_not_selected);
                return;
            }

            #endregion

            _currentLog = CreateLogFileForApp(apkFile);

            var currentCulture = Thread.CurrentThread.CurrentUICulture;
            Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.CurrentCulture = currentCulture;
                Thread.CurrentThread.CurrentUICulture = currentCulture;

                using (CreateWorking())
                {
                    try
                    {
                        Start();
                    }
                    catch (PathTooLongException ex)
                    {
                        HaveError(Environment.NewLine + ex, MainResources.PathTooLongExceptionMessage);
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Debug.WriteLine(ex.ToString());
                        throw;
#else
                        GlobalVariables.ErrorClient.Notify(ex);
                        HaveError(Environment.NewLine + ex, MainResources.Some_Error_Found);
#endif
                    }
                    finally
                    {
                        _currentLog?.Close();
                        _currentLog = null;
                    }
                }
            });
        }

        #endregion

        #region Button Drag & Drop handlers

        private void Apk_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".apk");
        }

        private void Apk_DragDrop(object sender, DragEventArgs e)
        {
            e.DropOneByEnd(".apk", file => ViewModel.CurrentApk.Value = file);
        }

        private void Save_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".tar.gz");
        }

        private void Save_DragDrop(object sender, DragEventArgs e)
        {
            e.DropOneByEnd(".tar.gz", file => ViewModel.CurrentSave.Value = file);
        }

        #endregion

        #region Menu element handlers

        private void InstallApkClick(object sender, RoutedEventArgs e)
        {
            _installApkWindowProvider.Get().ShowDialog();
        }

        private void ChangeLanguageClick(object sender, RoutedEventArgs e)
        {
            _settings.Language = sender.As<FrameworkElement>().Tag.As<string>();
            _applicationUtils.SetLanguageFromSettings();

            _shutdownOnClose = false;

            Close();
            _mainWindowProvider.Get().Show();
        }

        private void AboutProgramItem_Click(object sender, EventArgs e)
        {
            _aboutWindowProvider.Get().ShowDialog();
        }

#endregion

        private void Start()
        {
            Dispatcher.Invoke(LogBox.Clear);

            Log(
                string.Format(
                    "{0}{1}Start{1}{0}ExePath = {2}{0}Resources = {3}",
                    Environment.NewLine,
                    Line,
                    _globalVariables.PathToExe,
                    _globalVariables.PathToResources
                )
            );

            const int totalSteps = 3;

            _visualProgress.SetBarUsual();
            _visualProgress.ShowBar();

            _taskBarManager.SetProgress(0);
            _taskBarManager.SetUsualState();

            void SetStep(int currentStep, string status)
            {
                int percentage = (currentStep - 1) * 100 / totalSteps;

                _visualProgress.SetBarValue(percentage);
                _visualProgress.SetLabelText(status);
                _taskBarManager.SetProgress(percentage);
            }

            #region Инициализация

            SetStep(1, MainResources.StepInitializing);
            _visualProgress.ShowIndeterminateLabel();

            string sourceApkPath = ViewModel.CurrentApk.Value;
            bool alternativeSigning = _settings.AlternativeSigning;

            string popupText = ViewModel.PopupBoxText.Value;
            int messagesCount = ViewModel.MessagesCount.Value;

            bool needSave;
            bool needMessage;
            {
                bool onlySave = ViewModel.OnlySave.Value;
                bool savePlusMessage = ViewModel.SavePlusMess.Value;
                bool onlyMessage = ViewModel.OnlyMess.Value;

                needSave = onlySave || savePlusMessage;
                needMessage = (savePlusMessage || onlyMessage) && !string.IsNullOrEmpty(popupText) && messagesCount > 0;
            }

            BackupType backupType = ViewModel.BackupType;

            ITempFileProvider tempFileProvider = _tempUtils.CreateTempFileProvider();
            ITempFolderProvider tempFolderProvider = _tempUtils.CreateTempFolderProvider();

            string resultApkPath = sourceApkPath.Remove(sourceApkPath.Length - Path.GetExtension(sourceApkPath).Length) + "_mod.apk";
            string pathToSave = ViewModel.CurrentSave.Value;

            IApktool apktool = _apktoolProvider.Get();
            IProcessDataHandler dataHandler = new ProcessDataCombinedHandler(Log);

            #endregion

            #region Изменение apk

            using (var tempApk = ATempUtils.UseTempFile(tempFileProvider))
            {
                LFile.Copy(sourceApkPath, tempApk.TempFile, true);

                #region Добавление данных

                SetStep(2, MainResources.StepAddingData);

                var aes = new AesManaged {KeySize = 128};
                aes.GenerateIV();
                aes.GenerateKey();

                bool backupFilesAdded = false;
                // adding local and external backup files
                if (needSave)
                {
                    using (var internalDataBackup = ATempUtils.UseTempFile(tempFileProvider))
                    using (var externalDataBackup = ATempUtils.UseTempFile(tempFileProvider))
                    {
                        ApkModifer.ParseBackup(
                            pathToBackup: pathToSave,
                            backupType: backupType,
                            resultInternalDataPath: internalDataBackup.TempFile,
                            resultExternalDataPath: externalDataBackup.TempFile,
                            tempFolderProvider: tempFolderProvider
                        );

                        string internalBackup = internalDataBackup.TempFile;
                        string externalBackup = externalDataBackup.TempFile;

                        var fileToAssetsName = new Dictionary<string, string>
                        {
                            {internalBackup, "data.save"},
                            {externalBackup, "extdata.save"}
                        };

                        foreach (var (file, assetsName) in fileToAssetsName.Enumerate())
                        {
                            if (!LFile.Exists(file) || FileUtils.FileLength(file) == 0)
                                continue;

                            using (var tempEncrypted = ATempUtils.UseTempFile(tempFileProvider))
                            {
                                CommonUtils.EncryptFile(
                                    filePath: file,
                                    outputPath: tempEncrypted.TempFile,
                                    iv: aes.IV,
                                    key: aes.Key
                                );

                                ApkModifer.AddFileToZip(
                                    zipPath: tempApk.TempFile,
                                    filePath: tempEncrypted.TempFile,
                                    pathInZip: "assets/" + assetsName,
                                    newEntryCompression: CompressionType.Store
                                );
                            }

                            backupFilesAdded = true;
                        }
                    }
                }

                // adding smali file for restoring
                if (backupFilesAdded || needMessage)
                {
                    using (var decompiledFolder = ATempUtils.UseTempFolder(tempFolderProvider))
                    {
                        apktool.Baksmali(
                            apkPath: tempApk.TempFile,
                            resultFolder: decompiledFolder.TempFolder,
                            tempFolderProvider: tempFolderProvider,
                            dataHandler: dataHandler
                        );

                        var manifestPath = Path.Combine(decompiledFolder.TempFolder, "AndroidManifest.xml");

                        apktool.ExtractSimpleManifest(
                            apkPath: tempApk.TempFile,
                            resultManifestPath: manifestPath,
                            tempFolderProvider: tempFolderProvider
                        );

                        // have to have smali folders in the same directory as manifest
                        // to find the main smali
                        var manifest = new AndroidManifest(manifestPath);

                        if (manifest.MainSmaliFile == null)
                            throw new Exception("main smali file not found");

                        // using this instead of just pasting "folder/smali" as there can be
                        // no smali folder sometimes (smali_1, etc)
                        string smaliDir = manifest.MainSmaliPath.Substring(decompiledFolder.TempFolder.Length + 1);
                        smaliDir = smaliDir.Substring(0, smaliDir.IndexOf(Path.DirectorySeparatorChar));

                        string saveGameDir = Path.Combine(decompiledFolder.TempFolder, smaliDir, "com", "savegame");

                        LDirectory.CreateDirectory(saveGameDir);

                        CommonUtils.GenerateAndSaveSmali(
                            filePath: Path.Combine(saveGameDir, "SavesRestoringPortable.smali"),
                            iv: aes.IV,
                            key: aes.Key,
                            addSave: backupFilesAdded,
                            message: needMessage ? popupText : string.Empty,
                            messagesCount: needMessage ? messagesCount : 0
                        );

                        manifest.MainSmaliFile.AddTextToMethod(FileResources.MainSmaliCall);
                        manifest.MainSmaliFile.Save();

                        using (var folderWithDexes = ATempUtils.UseTempFolder(tempFolderProvider))
                        {
                            apktool.Smali(
                                folderWithSmali: decompiledFolder.TempFolder,
                                resultFolder: folderWithDexes.TempFolder,
                                dataHandler: dataHandler
                            );

                            string[] dexes = LDirectory.GetFiles(folderWithDexes.TempFolder, "*.dex");

                            ApkModifer.AddFilesToZip(
                                zipPath: tempApk.TempFile,
                                filePaths: dexes,
                                pathsInZip: Array.ConvertAll(dexes, Path.GetFileName),
                                newEntryCompression: CompressionType.Store
                            );
                        }
                    }
                }

                #endregion

                #region Подпись

                SetStep(3, MainResources.StepSigning);

                Log(Line);
                Log(MainResources.StepSigning);
                Log(Line);

                apktool.Sign(
                    sourceApkPath: tempApk.TempFile,
                    signedApkPath: resultApkPath,
                    tempFileProvider: tempFileProvider,
                    dataHandler: dataHandler,
                    deleteMetaInf: !alternativeSigning
                );

                #endregion
            }

            #endregion

            _visualProgress.HideIndeterminateLabel();
            SetStep(4, MainResources.AllDone);
            Log(MainResources.AllDone);

            if (_settings.Notifications)
            {
                _notificationManager.Show(
                    title: MainResources.Information_Title,
                    text: MainResources.ModificationCompletedContent
                );
            }

            if (
                MessBox.ShowDial(
                    MainResources.Path_to_file + resultApkPath, 
                    MainResources.Successful,
                    MainResources.OK, MainResources.Open
                ) == MainResources.Open)
            {
                Process.Start("explorer.exe", $"/select,{resultApkPath}");
            }

            _visualProgress.HideBar();
            _taskBarManager.SetNoneState();
        }

        private async Task CheckJavaVersion()
        {
            if (LDirectory.Exists(_globalVariables.PathToPortableJre))
                return;

            MessBox.ShowDial(
                MainResources.JavaInvalidVersion,
                MainResources.Information_Title,
                MainResources.OK
            );

            _visualProgress.SetBarValue(0);

            using (CreateWorking())
            {
                _visualProgress.SetBarUsual();
                _visualProgress.ShowBar();

                await _utils.DownloadJava(_visualProgress);

                _visualProgress.HideBar();
            }
        }

        public void Log(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            _currentLog?.WriteLine(text);

            Application.Current.Dispatcher.Invoke(() =>
            {
                LogBox.AppendText(text + Environment.NewLine);
                LogBox.ScrollToEnd();
            });
        }

        private void HaveError(string errorText, string dialogMessage = null)
        {
            Log($"{MainResources.Error}: {errorText}");

            if (string.IsNullOrEmpty(dialogMessage))
                return;

            MessBox.ShowDial(dialogMessage, MainResources.Error);
        }

        private void ChangeTheme_OnClick(object sender, RoutedEventArgs e)
        {
            var theme = sender.As<FrameworkElement>().Tag.As<string>();

            ThemeUtils.SetTheme(theme);
            _settings.Theme = theme;
        }

        private static StreamWriter CreateLogFileForApp(string pathToApkFile)
        {
            string apkDir = Path.GetDirectoryName(pathToApkFile) ?? string.Empty;

            string GenLogName(int index)
            {
                string logStart = Path.Combine(apkDir, $"{Path.GetFileNameWithoutExtension(pathToApkFile)}_log");

                return logStart + (index == 1 ? ".txt" : $" ({index}).txt");
            }

            int i = 1;
            while (true)
            {
                try
                {
                    return new StreamWriter(GenLogName(i++), false, Encoding.UTF8);
                }
                catch (Exception
#if !DEBUG
                    ex
#endif
                )
                {
                    if (i <= LogCreationTries)
                        continue;

#if !DEBUG
                    GlobalVariables.ErrorClient.Notify(ex);
#else
                    throw;
#endif
                }
            }
        }

#region Disposables

        private CustomBoolDisposable CreateWorking()
        {
            return new CustomBoolDisposable(val =>
            {
                ViewModel.Working.Value = val;
            });
        }

#endregion
    }
}
