using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using AndroidHelper.Logic;
using ApkModifer.Logic;
using ICSharpCode.SharpZipLib.Zip;
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

        private static readonly Encoding DefaultSmaliEncoding = new UTF8Encoding(false);
        private static readonly string Line = new string('-', 50);

        private readonly DefaultSettingsContainer _settings = DefaultSettingsContainer.Instance;

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
            await ApplicationUtils.CheckProVersion();
            await CheckJavaVersion();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            if (ViewModel.Pro.Value)
            {
                _settings.PopupMessage = ViewModel.PopupBoxText.Value;
            }

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

        private void BuyItem_Click(object sender, EventArgs e)
        {
            new ActivateProgramWindow().ShowDialog();
        }

        private void AboutProgramItem_Click(object sender, EventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

#endregion

        private void Start()
        {
            var apkFile = new FileInfo(ViewModel.CurrentApk.Value);
            bool pro = ViewModel.Pro.Value;
            bool alternativeSigning = _settings.AlternativeSigning;

            bool onlySave = ViewModel.OnlySave.Value;
            bool savePlusMessage = ViewModel.SavePlusMess.Value;
            bool onlyMessage = ViewModel.OnlyMess.Value;

            string popupText = ViewModel.PopupBoxText.Value;
            string mainSmali = ViewModel.MainSmaliName.Value;
            int messagesCount = ViewModel.MessagesCount.Value;

            BackupType backupType = ViewModel.BackupType;

            var tempFolder = Path.Combine(Path.GetTempPath(), "STG_temp");

            var processedApkPath = Path.Combine(tempFolder, "processed.apk");
            var resultApkPath = apkFile.GetFullFNWithoutExt() + "_mod.apk";
            var pathToSave = ViewModel.CurrentSave.Value;

            string pathToJava = GlobalVariables.PathToPortableJavaExe;
            if (!File.Exists(pathToJava))
                pathToJava = null;

            IOUtils.DeleteDir(tempFolder);
            IOUtils.CreateDir(tempFolder);

            const int totalSteps = 7;

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

            ViewModel.MainSmaliName.Value = ViewModel.MainSmaliName.Value.Replace('.', '\\').Replace('/', '\\');

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

#if DEBUG
            var apktool = new Apktools(processedApkPath, GlobalVariables.PathToResources, javaExePath: pathToJava, tracing: true);
#else
            var apktool = new Apktools(processedApkPath, GlobalVariables.PathToResources, javaExePath: pathToJava);
#endif

            apktool.Logging += Log;

            var apkmodifer = new ApkModifer.Logic.ApkModifer(apktool);

            string folderOfProject = apktool.FolderOfProject;

#endregion

#region Декомпиляция
            
            SetStep(2, MainResources.StepDecompiling);

            Log(Line);
            Log(MainResources.StepDecompiling);
            Log(Line);
            
            apktool.Baksmali();

            apktool.Manifest = apkmodifer.Apktools.GetSimpleManifest();

#endregion

#region Замена текстов

            SetStep(3, MainResources.StepReplacingTexts);

            if (pro)
            {
                var texts = new List<string> {" A P K M A N I A . C O M ", "Thanks for visiting APKMANIA.COM"};

                string messageFile = Path.Combine(GlobalVariables.PathToExeFolder, "Messages.txt");

                if (File.Exists(messageFile))
                {
                    texts.AddRange(File.ReadLines(messageFile, Encoding.UTF8).Where(line => !string.IsNullOrWhiteSpace(line)));
                }

                ReplaceTexts(Path.Combine(folderOfProject, "smali"), texts, Utils.EncodeUnicode(ViewModel.PopupBoxText.Value));
            }

#endregion

#region Удаление известных баннеров

            SetStep(4, MainResources.StepRemovingBanners);

            if (pro)
            {
                var bannersFile = Path.Combine(GlobalVariables.PathToExeFolder, "banners.txt");

                if (File.Exists(bannersFile))
                {
                    var banners = File.ReadLines(bannersFile).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

                    if (banners.Count == 0)
                    {
                        banners.AddRange(new []
                        {
                            "invoke-static {p0}, Lcom/apkmania/apkmania;->createInfoBox(Landroid/content/Context;)V",
                            "invoke-static {p0}, LNORLAN/Box/Message;->NORLANBoxMessage(Landroid/content/Context;)V"
                        });
                    }

                    RemoveCodeLines(Path.Combine(folderOfProject, "smali"), banners);
                }
            }

#endregion

#region Добавление данных

            SetStep(5, MainResources.StepAddingData);

            if (!string.IsNullOrEmpty(mainSmali))
            {
                var mainSmaliPath = Path.Combine(folderOfProject, "smali", mainSmali);

                if (!mainSmali.EndsWith(".smali", StringComparison.Ordinal))
                    mainSmaliPath += ".smali";

                if (File.Exists(mainSmaliPath))
                    apktool.Manifest.MainSmaliFile = new MainSmali(mainSmaliPath, apktool.Manifest.MainSmaliFile.MethodType, DefaultSmaliEncoding);
                else
                    Log($"Typed main smali was not found ({mainSmaliPath})");
            }

            AesManaged mng = new AesManaged { KeySize = 128 };

            mng.GenerateIV();
            mng.GenerateKey();

            apkmodifer.AddSaveAndMessage(
                iv: mng.IV,
                key: mng.Key, 
                addSave: onlySave || savePlusMessage,
                addMessage: savePlusMessage || onlyMessage,
                pathToSave: pathToSave,
                message: popupText,
                messagesAmount: messagesCount, 
                forceMethod: true, 
                backupType: backupType
            );

#endregion

#region Сборка проекта

            SetStep(6, MainResources.StepCompiling);

			Log(Line);
			Log(MainResources.StepCompiling);
			Log(Line);

            var classesFiles = apktool.Smali();

            ReplaceFilesInApk(apktool.FileName, classesFiles);

#endregion

#region Подпись

            SetStep(7, MainResources.StepSigning);

            Log(Line);
            Log(MainResources.StepSigning);
            Log(Line);

            string signed;

            if (!apktool.Sign(apktool.FileName, out signed, deleteMetaInf: !alternativeSigning))
            {
                HaveError("Error while signing", "Error while signing");
                return;
            }

            File.Copy(signed, resultApkPath, true);

#endregion

            IOUtils.DeleteDir(tempFolder);

            _visualProgress.HideIndeterminateLabel();
            SetStep(8, MainResources.AllDone);
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

        private static void ReplaceTexts(string folderWithSmaliFiles, IList<string> itemsToReplace, string targetString)
        {
            var encodedText = Utils.EncodeUnicode(targetString);

            var files = Directory.EnumerateFiles(folderWithSmaliFiles, "*.smali", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string fileText = File.ReadAllText(file, DefaultSmaliEncoding);

                bool changed = false;

                foreach (var text in itemsToReplace)
                {
                    var fullText = $"\"{text}\"";

                    if (!fileText.Contains(fullText))
                        continue;

                    fileText = fileText.Replace(fullText, $"\"{encodedText}\"");

                    changed = true;
                }

                if (changed)
                {
                    File.WriteAllText(file, fileText, DefaultSmaliEncoding);
                }
            }
        }

        private static void RemoveCodeLines(string folderWithSmaliFiles, IList<string> linesToRemove)
        {
            var files = Directory.EnumerateFiles(folderWithSmaliFiles, "*.smali", SearchOption.AllDirectories);

            foreach (var name in files)
            {
                string fileText = File.ReadAllText(name, DefaultSmaliEncoding);

                bool changed = false;

                foreach (var line in linesToRemove)
                {
                    if (!fileText.Contains(line))
                        continue;

                    fileText = fileText.Remove(fileText.IndexOf(line, StringComparison.Ordinal), line.Length + 2);

                    changed = true;
                }

                if (changed)
                {
                    File.WriteAllText(name, fileText, DefaultSmaliEncoding);
                }
            }
        }

        private static void ReplaceFilesInApk(string pathToApk, IEnumerable<string> pathToFiles)
        {
            using (var apkFile = new ZipFile(pathToApk))
            {
                apkFile.BeginUpdate();

                foreach (var pathToFile in pathToFiles)
                {
                    var fileName = Path.GetFileName(pathToFile);

                    ZipEntry entry = apkFile.GetEntry(fileName);

                    apkFile.Delete(entry);
                    apkFile.Add(new StaticDiskDataSource(pathToFile), fileName, entry.CompressionMethod);
                }

                apkFile.CommitUpdate();
            }
        }

        /*private static void ReplaceFileInApk(string pathToApk, string pathToFile)
        {
            var fileName = Path.GetFileName(pathToFile);

            //DotNextZip
            //{
            //    using (var apkFile = new Ionic.Zip.ZipFile(pathToApk))
            //    {
            //        apkFile.RemoveSelectedEntries($"name = {fileName}");
            //        apkFile.AddFile(pathToFile, string.Empty);

            //        apkFile.Save();
            //    }
            //}

            // SharpZipLib
            {
                using (var apkFile = new ICSharpCode.SharpZipLib.Zip.ZipFile(pathToApk))
                {
                    apkFile.BeginUpdate();

                    apkFile.Delete(fileName);
                    apkFile.Add(pathToFile, fileName);

                    apkFile.CommitUpdate();
                }
            }

            //ZipStorer
            //{
            //    var zip = System.IO.Compression.ZipStorer.Open(apathToApk, FileAccess.ReadWrite);

            //    var classesEntry = zip.ReadCentralDir().FirstOrNull(entry => entry.FilenameInZip == fileName);

            //    if (classesEntry != null)
            //        System.IO.Compression.ZipStorer.RemoveEntries(ref zip,
            //            new List<System.IO.Compression.ZipStorer.ZipFileEntry> { classesEntry.Value });
            //    zip.AddFile(System.IO.Compression.ZipStorer.Compression.Store,
            //        pathToFile, fileName, string.Empty);

            //    zip.Close();
            //}
        }*/

        private async Task CheckJavaVersion()
        {
            if (Directory.Exists(GlobalVariables.PathToPortableJre))
                return;

            var (primary, secondary) = Utils.GetInstalledJavaVersion();

            if (primary == 1 && secondary >= 5 && secondary <= 8)
                return;

            var promtRes = MessBox.ShowDial(
                MainResources.JavaInvalidVersion,
                MainResources.Information_Title,
                MainResources.No,
                MainResources.Yes
            );

            if (promtRes != MainResources.Yes)
                return;

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
