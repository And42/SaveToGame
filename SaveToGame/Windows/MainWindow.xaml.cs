using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using AndroidLibs;
using Microsoft.Win32;
using SaveToGameWpf.Logic;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Resources.Localizations;
using UsefulClasses;
using UsefulFunctionsLib;

using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DragEventArgs = System.Windows.DragEventArgs;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using StrRes = SaveToGameWpf.Resources.Localizations.MainResources;
using IOHelper = UsefulFunctionsLib.UsefulFunctions_IOHelper;
using Path = Alphaleonis.Win32.Filesystem.Path;
using BackupType = ApkModifer.ApkModifer.BackupType;

namespace SaveToGameWpf.Windows
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : IRaisePropertyChanged, IDisposable
    {
        #region Properties     

        public bool Pro
        {
            get => _pro;
            set
            {
                if (this.SetProperty(ref _pro, value))
                    RaisePropertyChanged(nameof(MainWindowTitle));
            }
        }
        private bool _pro;

        public bool Working
        {
            get => _working;
            set => this.SetProperty(ref _working, value);
        }
        private bool _working;

        public bool OnlySave
        {
            get => _onlySave;
            set => this.SetProperty(ref _onlySave, value);
        }
        private bool _onlySave;

        public bool SavePlusMess
        {
            get => _savePlusMess;
            set => this.SetProperty(ref _savePlusMess, value);
        }
        private bool _savePlusMess = true;

        public bool OnlyMess
        {
            get => _onlyMess;
            set => this.SetProperty(ref _onlyMess, value);
        }
        private bool _onlyMess;

        public string PopupBoxText
        {
            get => _popupBoxText;
            set => this.SetProperty(ref _popupBoxText, value);
        }
        private string _popupBoxText = "Modified by SaveToGame";

        public int MessagesCount
        {
            get => _messagesCount;
            set => this.SetProperty(ref _messagesCount, value);
        }
        private int _messagesCount = 1;

        public string MainSmaliName
        {
            get => _mainSmaliName;
            set => this.SetProperty(ref _mainSmaliName, value);
        }
        private string _mainSmaliName = "";

        public string CurrentApk
        {
            get => _currentApk;
            set
            {
                if (this.SetProperty(ref _currentApk, value))
                {
                    _currentApkFile = new FileInfo(value);
                    RaisePropertyChanged(nameof(MainWindowTitle));
                }
            }
        }
        private string _currentApk;

        private FileInfo _currentApkFile;

        public string CurrentSave { get; set; }

        public static readonly Encoding DefaultSmaliEncoding = new UTF8Encoding(false);

        public bool RuIsChecked => Thread.CurrentThread.CurrentCulture.ToString().Contains("ru");
        public bool EnIsChecked => Thread.CurrentThread.CurrentCulture.ToString().Contains("en");

        public bool TitaniumIsChecked
        {
            get => CurrentBackupType == BackupType.Titanium;
            set
            {
                if (value)
                    CurrentBackupType = BackupType.Titanium;
            }
        }

        public bool RomToolboxIsChecked
        {
            get => CurrentBackupType == BackupType.RomToolbox;
            set
            {
                if (value)
                    CurrentBackupType = BackupType.RomToolbox;
            } 
        }

        public bool LuckyPatcherIsChecked
        {
            get => CurrentBackupType == BackupType.LuckyPatcher;
            set
            {
                if (value)
                    CurrentBackupType = BackupType.LuckyPatcher;
            }
        }

        public string MainWindowTitle
            => Properties.Resources.AppName + (Pro ? " Pro" : "") + (CurrentApk != null ? " - " + CurrentApk : "");

        public BackupType CurrentBackupType
        {
            get => SettingsIncapsuler.BackupType;
            set
            {
                SettingsIncapsuler.BackupType = value;

                RaisePropertyChanged(nameof(CurrentBackupType));

                RaisePropertyChanged(nameof(TitaniumIsChecked));
                RaisePropertyChanged(nameof(RomToolboxIsChecked));
                RaisePropertyChanged(nameof(LuckyPatcherIsChecked));
            }
        }

        #endregion

        private static readonly string Line = new string('-', 50);

        private Logger _logger;

        private bool _shutdownOnClose = true;

        static MainWindow()
        {
#if !DEBUG
            if (!ApplicationUtils.GetIsPortable())
#endif
                ApplicationUtils.CheckForUpdate();
        }

		public MainWindow()
		{
            string lang = SettingsIncapsuler.Language;

            if (!string.IsNullOrEmpty(lang))
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(lang);
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
            }

            InitializeComponent();

            TaskbarItemInfo = new TaskbarItemInfo();
		}

        #region Window events

        private void MainWindow_Load(object sender, EventArgs e)
        {
            ApplicationUtils.LoadSettings();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            if (Pro)
            {
                SettingsIncapsuler.PopupMessage = PopupBoxText;
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
            var openApkDialog = new OpenFileDialog
            {
                Filter = MainResources.AndroidFiles + @" (*.apk)|*.apk",
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openApkDialog.ShowDialog() == true)
            {
                CurrentApk = openApkDialog.FileName;
                ChooseApkButton.ToolTip = CurrentApk;
            }
        }

        private void ChooseSaveBtn_Click(object sender, EventArgs e)
        {
            var openSaveDialog = new OpenFileDialog
            {
                Filter = MainResources.Archives + @" (*.tar.gz)|*.tar.gz",
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (!LuckyPatcherIsChecked)
            {
                if (openSaveDialog.ShowDialog() == true)
                    CurrentSave = openSaveDialog.FileName;
            }
            else
            {
                var (res, folderPath) = Utils.OpenFolderWithDialog();

                if (res)
                {
                    CurrentSave = folderPath;
                }
            }

            ChooseSaveButton.ToolTip = CurrentSave;
        }

        private void StartBtn_Click(object sender, EventArgs e)
        {
            #region Проверка на наличие Java

            if (!Apktools.StaticHasJava())
            {
                Clipboard.SetText("http://www.oracle.com/technetwork/java/javase/downloads/jdk7-downloads-1880260.html");
                MessBox.ShowDial(Properties.Resources.JavaNotFound);
                return;
            }

            #endregion

            #region Проверка на существование файлов

            if (CurrentApk == null || !File.Exists(CurrentApk) ||
                (SavePlusMess || OnlySave) &&
                (CurrentSave == null || !File.Exists(CurrentSave) && !Directory.Exists(CurrentSave))
            )
            {
                MessBox.ShowDial(Properties.Resources.File_or_save_not_selected, Properties.Resources.Error);
                return;
            }

            #endregion

            _logger = new Logger(_currentApkFile.DirectoryName, false);
            _logger.NewLog(true, Path.Combine(_currentApkFile.DirectoryName ?? string.Empty, $"{Path.GetFileNameWithoutExtension(CurrentApk)}_log.txt"));

            Working = true;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    Start();
                }
                catch (System.IO.PathTooLongException ex)
                {
                    HaveError(Environment.NewLine + ex, StrRes.PathTooLongExceptionMessage);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Dispatcher.InvokeAction(() =>
                    {
                        TraceWriter.WriteLine(ex.ToString());
                        throw new Exception("Some exception occured", ex);
                    });
#else
                    HaveError(Environment.NewLine + ex);
		            MessBox.ShowDial(Properties.Resources.Some_Error_Found);
#endif
                }
                finally
                {
                    _logger.Stop();
                    Dispatcher.InvokeAction(() => TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None);
                }

                Working = false;
            });
        }

        #endregion

        #region Button drag&drop handlers

        private void Apk_DragOver(object sender, DragEventArgs e)
        {
            Utils.CheckDragOver(e, ".apk");
        }

        private void Apk_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length == 1 && Path.GetExtension(files[0]) == ".apk")
                CurrentApk = files[0];
            e.Handled = true;
        }

        private void Save_DragOver(object sender, DragEventArgs e)
        {
            Utils.CheckDragOver(e, ".tar.gz");
        }

        private void Save_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length == 1 && files[0].EndsWith(".tar.gz", StringComparison.Ordinal))
                CurrentSave = files[0];
            e.Handled = true;
        }

        #endregion

        #region Menu element handlers

        private void InstallApkClick(object sender, RoutedEventArgs e)
        {
            WindowManager.ActivateWindow<InstallApkWindow>(this);
        }

        private void ChangeLanguageClick(object sender, RoutedEventArgs e)
        {
            SettingsIncapsuler.Language = sender.As<FrameworkElement>().Tag.As<string>();

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
            var tempFolder = Path.Combine(Path.GetTempPath(), "STG_temp");

            var processedApkPath = Path.Combine(tempFolder, "processed.apk");
            var resultApkPath = _currentApkFile.GetFullFNWithoutExt() + "_mod.apk";
            var pathToSave = CurrentSave;

            Utils.DeleteFolder(tempFolder);
            Directory.CreateDirectory(tempFolder);

            #region Подготовка

            Dispatcher.InvokeAction(() =>
            {
                SlashChange();
                LogBox.Clear();
            });

            #endregion

            #region Запись начала в лог

            Log(string.Format("{0}{1}Start{1}{0}ExePath = {2}{0}Resources = {3}", Environment.NewLine, Line, GlobalVariables.PathToExe, GlobalVariables.PathToResources));

            #endregion

            #region Инициализация

            File.Copy(CurrentApk, processedApkPath, true);

#if DEBUG
            var apktool = new Apktools(processedApkPath, GlobalVariables.PathToResources, tracing: true);
#else
            var apktool = new Apktools(processedApkPath, GlobalVariables.PathToResources);
#endif

            apktool.Logging += LogLib;

            var apkmodifer = new ApkModifer.ApkModifer(apktool);

            string folderOfProject = apktool.FolderOfProject;

            #endregion

            #region Декомпиляция
            
            Log(VisLog(Line));
            Log(VisLog("Decompiling"));
            Log(VisLog(Line));
            
            apktool.Baksmali();

            apktool.Manifest = apkmodifer.Apktools.GetSimpleManifest();

            #endregion

            #region Замена текстов

            if (Pro)
            {
                var texts = new List<string> {" A P K M A N I A . C O M ", "Thanks for visiting APKMANIA.COM"};

                string messageFile = Path.Combine(GlobalVariables.PathToExeFolder, "Messages.txt");

                if (File.Exists(messageFile))
                {
                    texts.AddRange(File.ReadLines(messageFile, Encoding.UTF8));
                }

                ReplaceTexts(Path.Combine(folderOfProject, "smali"), texts, Utils.EncodeUnicode(PopupBoxText));
            }

            #endregion

            #region Удаление известных баннеров

            if (Pro)
            {
                var bannersFile = Path.Combine(GlobalVariables.PathToExeFolder, "banners.txt");

                if (File.Exists(bannersFile))
                {
                    var banners = File.ReadAllLines(bannersFile);

                    if (banners.Length == 0)
                    {
                        banners = new[]
                        {
                            "invoke-static {p0}, Lcom/apkmania/apkmania;->createInfoBox(Landroid/content/Context;)V",
                            "invoke-static {p0}, LNORLAN/Box/Message;->NORLANBoxMessage(Landroid/content/Context;)V"
                        };
                    }

                    RemoveCodeLines(Path.Combine(folderOfProject, "smali"), banners);
                }
            }

            #endregion

            #region Добавление данных

            if (!string.IsNullOrEmpty(MainSmaliName))
            {
                var mainSmaliPath = Path.Combine(folderOfProject, "smali", MainSmaliName);

                if (!MainSmaliName.EndsWith(".smali", StringComparison.Ordinal))
                    mainSmaliPath += ".smali";

                if (File.Exists(mainSmaliPath))
                    apktool.Manifest.MainSmaliFile = new MainSmali(mainSmaliPath, apktool.Manifest.MainSmaliFile.MethodType, DefaultSmaliEncoding);
                else
                    Log(VisLog($"Typed main smali was not found ({mainSmaliPath})"));
            }

            AesManaged mng = new AesManaged { KeySize = 128 };

            mng.GenerateIV();
            mng.GenerateKey();

            var backupTypeDict = new List<(bool typeIsChecked, BackupType backupType)>
            {
                (TitaniumIsChecked, BackupType.Titanium),
                (RomToolboxIsChecked, BackupType.RomToolbox),
                (LuckyPatcherIsChecked, BackupType.LuckyPatcher)
            };

            var backupType = backupTypeDict.First(it => it.typeIsChecked).backupType;

            apkmodifer.AddSaveAndMessage(
                iv: mng.IV,
                key: mng.Key, 
                addSave: OnlySave || SavePlusMess,
                addMessage: SavePlusMess || OnlyMess,
                pathToSave: pathToSave,
                message: PopupBoxText,
                messagesAmount: MessagesCount, 
                forceMethod: true, 
                backupType: backupType
            );

            #endregion

            #region Сборка проекта

			Log(VisLog(Line));
			Log(VisLog("Building project"));
			Log(VisLog(Line));

            var classesFiles = apktool.Smali();

            ReplaceFilesInApk(apktool.FileName, classesFiles);

            #endregion

            Log(VisLog(Line));
            Log(VisLog("Signing file"));
            Log(VisLog(Line));

            #region Подпись

            string signed;

            if (!apktool.Sign(apktool.FileName, out signed))
            {
                HaveError("Error while signing", "Error while signing");
                return;
            }

            File.Copy(signed, resultApkPath, true);

            #endregion

            Utils.DeleteFolder(tempFolder);

            VisLog("All done!");

            if (
                MessBox.ShowDial(
                    Properties.Resources.Path_to_file + resultApkPath, 
                    Properties.Resources.Successful,
                    Properties.Resources.OK, Properties.Resources.Open
                ) == Properties.Resources.Open)
            {
                Process.Start("explorer.exe", $"/select,{resultApkPath}");
            }
        }

        private static void ReplaceTexts(string folderWithSmaliFiles, IList<string> itemsToReplace, string targetString)
        {
            var encodedText = Utils.EncodeUnicode(targetString);

            var files = Directory.EnumerateFiles(folderWithSmaliFiles, "*.smali", System.IO.SearchOption.AllDirectories);

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
            var files = Directory.EnumerateFiles(folderWithSmaliFiles, "*.smali", System.IO.SearchOption.AllDirectories);

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
            using (var apkFile = new ICSharpCode.SharpZipLib.Zip.ZipFile(pathToApk))
            {
                apkFile.BeginUpdate();

                foreach (var pathToFile in pathToFiles)
                {
                    var fileName = Path.GetFileName(pathToFile);

                    apkFile.Delete(fileName);
                    apkFile.Add(pathToFile, fileName);
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

        public string VisLog(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            Application.Current.Dispatcher.InvokeAction(() =>
            {
                LogBox.AppendText(text + Environment.NewLine);
                LogBox.ScrollToEnd();
            });

            return text;
        }

        public void LogLib(string textToLog)
        {
            Log(VisLog(textToLog));
        }

        public void Log(params string[] textToLog)
        {
            foreach (string text in textToLog)
            {
                _logger.Log(text);
                TraceWriter.WriteLine(text);
            }
        }

        private void HaveError(string errorText, string dialogMessage = null)
        {
            VisLog(errorText);
            Log($"error: {errorText}");
            if (dialogMessage != null)
            {
                Dispatcher.InvokeAction(() => MessBox.ShowDial(dialogMessage, Properties.Resources.Error));
            }
        }

		private void SlashChange()
		{
			MainSmaliName = MainSmaliName.Replace('.', '\\').Replace('/', '\\');
		}

        public void Dispose()
        {
            _logger.Dispose();
        }

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
