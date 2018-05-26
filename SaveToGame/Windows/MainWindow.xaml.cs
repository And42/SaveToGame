using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using AndroidHelper.Logic;
using ApkModifer.Logic;
using MVVM_Tools.Code.Disposables;
using MVVM_Tools.Code.Providers;
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
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DragEventArgs = System.Windows.DragEventArgs;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace SaveToGameWpf.Windows
{
    public sealed partial class MainWindow : IRaisePropertyChanged, IDisposable
    {
        #region Properties     

        public Property<bool> Pro { get; } = new Property<bool>();

        public Property<bool> Working { get; } = new Property<bool>();
        public Property<bool> OnlySave { get; } = new Property<bool>();
        public Property<bool> SavePlusMess { get; } = new Property<bool>();
        public Property<bool> OnlyMess { get; } = new Property<bool>();

        public Property<string> PopupBoxText { get; } = new Property<string>("Modified by SaveToGame");
        public Property<int> MessagesCount { get; } = new Property<int>(1);

        public Property<string> MainSmaliName { get; } = new Property<string>(string.Empty);

        public Property<string> CurrentApk { get; } = new Property<string>();
        public Property<string> CurrentSave { get; } = new Property<string>();

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
            => MainResources.AppName + (Pro.Value ? " Pro" : "") + (!string.IsNullOrEmpty(CurrentApk.Value) ? " - " + CurrentApk.Value : "");

        public BackupType CurrentBackupType
        {
            get => _settings.BackupType;
            set
            {
                _settings.BackupType = value;

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

        private readonly DefaultSettingsContainer _settings = DefaultSettingsContainer.Instance;

        static MainWindow()
        {
#if !DEBUG
            if (!ApplicationUtils.GetIsPortable())
#endif
                ApplicationUtils.CheckForUpdate();
        }

		public MainWindow()
		{
            InitializeComponent();

		    TaskbarItemInfo = new TaskbarItemInfo();

		    Pro.PropertyChanged += (sender, args) => RaisePropertyChanged(nameof(Pro));
		    CurrentApk.PropertyChanged += (sender, args) => RaisePropertyChanged(nameof(CurrentApk));

            PropertyChanged += OnPropertyChanged;
		}

        #region Window events

        private void MainWindow_Load(object sender, EventArgs e)
        {
            ApplicationUtils.LoadSettings();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            if (Pro.Value)
            {
                _settings.PopupMessage = PopupBoxText.Value;
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

            CurrentApk.Value = filePath;
            ChooseApkButton.ToolTip = filePath;
        }

        private void ChooseSaveBtn_Click(object sender, EventArgs e)
        {
            if (!LuckyPatcherIsChecked)
            {
                var (success, filePath) = PickerUtils.PickFile(filter: MainResources.Archives + @" (*.tar.gz)|*.tar.gz");

                if (success)
                    CurrentSave.Value = filePath;
            }
            else
            {
                var (success, folderPath) = PickerUtils.PickFolder();

                if (success)
                    CurrentSave.Value = folderPath;
            }
        }

        private void StartBtn_Click(object sender, EventArgs e)
        {
            var apkFile = CurrentApk.Value;
            var saveFile = CurrentSave.Value;

            #region Проверка на наличие Java

            if (!Apktools.StaticHasJava())
            {
                Clipboard.SetText("http://www.oracle.com/technetwork/java/javase/downloads/jdk7-downloads-1880260.html");
                MessBox.ShowDial(MainResources.JavaNotFound);
                return;
            }

            #endregion

            #region Проверка на существование файлов

            if (string.IsNullOrEmpty(apkFile) || !File.Exists(apkFile) ||
                (SavePlusMess.Value || OnlySave.Value) &&
                (string.IsNullOrEmpty(saveFile) || !File.Exists(saveFile) && !Directory.Exists(saveFile))
            )
            {
                MessBox.ShowDial(MainResources.File_or_save_not_selected, MainResources.Error);
                return;
            }

            #endregion

            var apkDir = Path.GetDirectoryName(apkFile);

            _logger = new Logger(apkDir, false);
            _logger.NewLog(true, Path.Combine(apkDir ?? string.Empty, $"{Path.GetFileNameWithoutExtension(apkFile)}_log.txt"));

            Task.Factory.StartNew(() =>
            {
                using (CreateWorking())
                {
                    try
                    {
                        Start();
                    }
                    catch (System.IO.PathTooLongException ex)
                    {
                        HaveError(Environment.NewLine + ex, MainResources.PathTooLongExceptionMessage);
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
            e.DropOneByEnd(".apk", file => CurrentApk.Value = file);
        }

        private void Save_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".tar.gz");
        }

        private void Save_DragDrop(object sender, DragEventArgs e)
        {
            e.DropOneByEnd(".tar.gz", file => CurrentSave.Value = file);
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
            var apkFile = new FileInfo(CurrentApk.Value);
            bool pro = Pro.Value;

            var tempFolder = Path.Combine(Path.GetTempPath(), "STG_temp");

            var processedApkPath = Path.Combine(tempFolder, "processed.apk");
            var resultApkPath = apkFile.GetFullFNWithoutExt() + "_mod.apk";
            var pathToSave = CurrentSave.Value;

            Utils.DeleteFolder(tempFolder);
            Directory.CreateDirectory(tempFolder);

            #region Подготовка

            Dispatcher.InvokeAction(() =>
            {
                MainSmaliName.Value = MainSmaliName.Value.Replace('.', '\\').Replace('/', '\\');
                LogBox.Clear();
            });

            #endregion

            #region Запись начала в лог

            Log(string.Format("{0}{1}Start{1}{0}ExePath = {2}{0}Resources = {3}", Environment.NewLine, Line, GlobalVariables.PathToExe, GlobalVariables.PathToResources));

            #endregion

            #region Инициализация

            File.Copy(apkFile.FullName, processedApkPath, true);

#if DEBUG
            var apktool = new Apktools(processedApkPath, GlobalVariables.PathToResources, tracing: true);
#else
            var apktool = new Apktools(processedApkPath, GlobalVariables.PathToResources);
#endif

            apktool.Logging += Log;

            var apkmodifer = new ApkModifer.Logic.ApkModifer(apktool);

            string folderOfProject = apktool.FolderOfProject;

            #endregion

            #region Декомпиляция
            
            Log(Line);
            Log("Decompiling");
            Log(Line);
            
            apktool.Baksmali();

            apktool.Manifest = apkmodifer.Apktools.GetSimpleManifest();

            #endregion

            #region Замена текстов

            if (pro)
            {
                var texts = new List<string> {" A P K M A N I A . C O M ", "Thanks for visiting APKMANIA.COM"};

                string messageFile = Path.Combine(GlobalVariables.PathToExeFolder, "Messages.txt");

                if (File.Exists(messageFile))
                {
                    texts.AddRange(File.ReadLines(messageFile, Encoding.UTF8).Where(line => !string.IsNullOrWhiteSpace(line)));
                }

                ReplaceTexts(Path.Combine(folderOfProject, "smali"), texts, Utils.EncodeUnicode(PopupBoxText.Value));
            }

            #endregion

            #region Удаление известных баннеров

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

            if (!string.IsNullOrEmpty(MainSmaliName.Value))
            {
                var mainSmaliPath = Path.Combine(folderOfProject, "smali", MainSmaliName.Value);

                if (!MainSmaliName.Value.EndsWith(".smali", StringComparison.Ordinal))
                    mainSmaliPath += ".smali";

                if (File.Exists(mainSmaliPath))
                    apktool.Manifest.MainSmaliFile = new MainSmali(mainSmaliPath, apktool.Manifest.MainSmaliFile.MethodType, DefaultSmaliEncoding);
                else
                    Log($"Typed main smali was not found ({mainSmaliPath})");
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
                addSave: OnlySave.Value || SavePlusMess.Value,
                addMessage: SavePlusMess.Value || OnlyMess.Value,
                pathToSave: pathToSave,
                message: PopupBoxText.Value,
                messagesAmount: MessagesCount.Value, 
                forceMethod: true, 
                backupType: backupType
            );

            #endregion

            #region Сборка проекта

			Log(Line);
			Log("Building project");
			Log(Line);

            var classesFiles = apktool.Smali();

            ReplaceFilesInApk(apktool.FileName, classesFiles);

            #endregion

            Log(Line);
            Log("Signing file");
            Log(Line);

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

            Log("All done!");

            if (
                MessBox.ShowDial(
                    MainResources.Path_to_file + resultApkPath, 
                    MainResources.Successful,
                    MainResources.OK, MainResources.Open
                ) == MainResources.Open)
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

        public void Log(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            _logger.Log(text);
            TraceWriter.WriteLine(text);

            Application.Current.Dispatcher.InvokeAction(() =>
            {
                LogBox.AppendText(text + Environment.NewLine);
                LogBox.ScrollToEnd();
            });
        }

        private void HaveError(string errorText, string dialogMessage = null)
        {
            Log($"Error: {errorText}");
            if (dialogMessage != null)
            {
                Dispatcher.InvokeAction(() => MessBox.ShowDial(dialogMessage, MainResources.Error));
            }
        }

        public void Dispose()
        {
            _logger.Dispose();
        }

        private CustomBoolDisposable CreateWorking()
        {
            return new CustomBoolDisposable(val =>
            {
                Working.Value = val;
                Dispatcher.InvokeAction(
                    () => TaskbarItemInfo.ProgressState = val 
                        ? TaskbarItemProgressState.Indeterminate
                        : TaskbarItemProgressState.None
                );
            });
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Pro):
                case nameof(CurrentApk):
                    RaisePropertyChanged(nameof(MainWindowTitle));
                    break;
            }
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
