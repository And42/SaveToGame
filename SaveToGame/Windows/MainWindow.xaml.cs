using System;
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
using ApkModifer.Logic;
using MVVM_Tools.Code.Disposables;
using SaveToGameWpf.Logic;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Logic.ViewModels;
using SaveToGameWpf.Resources.Localizations;

using Application = System.Windows.Application;
using DragEventArgs = System.Windows.DragEventArgs;

namespace SaveToGameWpf.Windows
{
    public sealed partial class MainWindow
    {
        // how many times app should try to create log file for the apk file processing
        private const int LogCreationTries = 50;

        private static readonly string Line = new string('-', 50);

        private readonly AppSettings _settings = AppSettings.Instance;

        private readonly IVisualProgress _visualProgress;
        private readonly ITaskBarManager _taskBarManager;

        public MainWindowViewModel ViewModel { get; }

        private StreamWriter _currentLog;

        private bool _shutdownOnClose = true;

        public MainWindow()
		{
		    ViewModel = new MainWindowViewModel();

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

            if (string.IsNullOrEmpty(apkFile) || !File.Exists(apkFile) ||
                (ViewModel.SavePlusMess.Value || ViewModel.OnlySave.Value) &&
                (string.IsNullOrEmpty(saveFile) || !File.Exists(saveFile) && !Directory.Exists(saveFile))
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
            WindowManager.ActivateWindow<InstallApkWindow>(this);
        }

        private void ChangeLanguageClick(object sender, RoutedEventArgs e)
        {
            _settings.Language = sender.As<FrameworkElement>().Tag.As<string>();
            ApplicationUtils.SetLanguageFromSettings();

            _shutdownOnClose = false;

            WindowManager.CloseWindow<MainWindow>();
            WindowManager.ActivateWindow<MainWindow>();
        }

        private void AboutProgramItem_Click(object sender, EventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

#endregion

        private void Start()
        {
            var apkFile = new FileInfo(ViewModel.CurrentApk.Value);
            bool alternativeSigning = _settings.AlternativeSigning;

            bool onlySave = ViewModel.OnlySave.Value;
            bool savePlusMessage = ViewModel.SavePlusMess.Value;
            bool onlyMessage = ViewModel.OnlyMess.Value;

            string popupText = ViewModel.PopupBoxText.Value;
            int messagesCount = ViewModel.MessagesCount.Value;

            BackupType backupType = ViewModel.BackupType;

            ITempFileProvider tempFileProvider = TempUtils.CreateTempFileProvider();
            ITempFolderProvider tempFolderProvider = TempUtils.CreateTempFolderProvider();
            var tempFolder = Path.Combine(Path.GetTempPath(), "STG_temp");

            var processedApkPath = Path.Combine(tempFolder, "processed.apk");
            var resultApkPath = apkFile.GetFullFNWithoutExt() + "_mod.apk";
            var pathToSave = ViewModel.CurrentSave.Value;

            IOUtils.DeleteDir(tempFolder);
            IOUtils.CreateDir(tempFolder);

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

#region Подготовка

            Dispatcher.InvokeAction(() => LogBox.Clear());

#endregion

#region Запись начала в лог

            Log(
                string.Format(
                    "{0}{1}Start{1}{0}ExePath = {2}{0}Resources = {3}",
                    Environment.NewLine,
                    Line,
                    GlobalVariables.PathToExe,
                    GlobalVariables.PathToResources
                )
            );

#endregion

#region Инициализация

            SetStep(1, MainResources.StepInitializing);
            _visualProgress.ShowIndeterminateLabel();

            File.Copy(apkFile.FullName, processedApkPath, true);

            IApktool apktool = new Apktool.Builder()
                .JavaPath(GlobalVariables.PathToPortableJavaExe)
                .ApktoolPath(GlobalVariables.ApktoolPath)
                .SignApkPath(GlobalVariables.SignApkPath)
                .BaksmaliPath(GlobalVariables.BaksmaliPath)
                .SmaliPath(GlobalVariables.SmaliPath)
                .DefaultKeyPemPath(GlobalVariables.DefaultKeyPemPath)
                .DefaultKeyPkPath(GlobalVariables.DefaultKeyPkPath)
                .Build();

            IProcessDataHandler dataHandler = new ProcessDataCombinedHandler(Log);

            string folderOfProject =
                Path.Combine(
                    Path.GetDirectoryName(processedApkPath),
                    Path.GetFileNameWithoutExtension(processedApkPath)
                );

#endregion

#region Добавление данных

            SetStep(2, MainResources.StepAddingData);

            {
                var apkModifer = new ApkModifer.Logic.ApkModifer(
                    apktool: apktool,
                    apkPath: processedApkPath,
                    tempFolderProvider: tempFolderProvider
                );

                var mng = new AesManaged {KeySize = 128};
                mng.GenerateIV();
                mng.GenerateKey();
                apkModifer.Encrypt(mng.IV, mng.Key);

                if (onlySave || savePlusMessage)
                    apkModifer.Backup(pathToSave, backupType);

                if (savePlusMessage || onlyMessage)
                    apkModifer.Message(popupText, messagesCount);

                apkModifer.Process();
            }

#endregion

#region Подпись

            SetStep(3, MainResources.StepSigning);

            Log(Line);
            Log(MainResources.StepSigning);
            Log(Line);

            apktool.Sign(
                sourceApkPath: processedApkPath,
                signedApkPath: resultApkPath,
                tempFileProvider: tempFileProvider,
                dataHandler: dataHandler,
                deleteMetaInf: !_settings.AlternativeSigning
            );

#endregion

            IOUtils.DeleteDir(tempFolder);

            _visualProgress.HideIndeterminateLabel();
            SetStep(4, MainResources.AllDone);
            Log(MainResources.AllDone);

            if (_settings.Notifications)
            {
                NotificationManager.Instance.Show(MainResources.Information_Title, MainResources.ModificationCompletedContent);
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
            if (Directory.Exists(GlobalVariables.PathToPortableJre))
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

                await Utils.DownloadJava(_visualProgress);

                _visualProgress.HideBar();
            }
        }

        public void Log(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            _currentLog?.WriteLine(text);

            Application.Current.Dispatcher.InvokeAction(() =>
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
            AppSettings.Instance.Theme = theme;
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
