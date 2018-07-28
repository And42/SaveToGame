using System;
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
using ApkModifer.Logic;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using MVVM_Tools.Code.Disposables;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Resources.Localizations;

using DragEventArgs = System.Windows.DragEventArgs;
using Image = System.Windows.Controls.Image;

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

        private readonly DefaultSettingsContainer _settings = DefaultSettingsContainer.Instance;

        private readonly StringBuilder _log = new StringBuilder();
        private readonly IVisualProgress _visualProgress;
        private readonly ITaskBarManager _taskBarManager;

        public InstallApkWindow()
        {
            var iconsFolder = Path.Combine(GlobalVariables.PathToResources, "icons");

            BitmapSource GetImage(string name) =>
                File.ReadAllBytes(Path.Combine(iconsFolder, name)).ToBitmap().ToBitmapSource();

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

            if (string.IsNullOrEmpty(apkFile) || !File.Exists(apkFile))
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
                    Dispatcher.InvokeAction(() => throw ex);
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
                string tag = e.OriginalSource.As<Image>().Tag.As<string>();
                SetIcon(file, tag);
            });
        }

        #endregion

        private void ProcessAll(byte[] xxhdpiBytes, byte[] xhdpiBytes, byte[] hdpiBytes, byte[] mdpiBytes)
        {
            string apkFile = Apk.Value;
            string saveFile = Save.Value;
            string androidDataFile = Data.Value;
            string[] androidObbFiles = (string[]) Obb.Value?.Clone() ?? new string[0];
            string appTitle = AppTitle.Value;

            _visualProgress.SetBarIndeterminate();
            _visualProgress.ShowBar();
            _visualProgress.ShowIndeterminateLabel();
            _taskBarManager.SetProgress(0);
            _taskBarManager.SetUsualState();
            SetStep(MainResources.StepInitializing, 1);

            var tempProcessedFolder = Path.Combine(Path.GetTempPath(), "STG_temp");

            var containerApkPath = Path.Combine(tempProcessedFolder, "apk.apk");
            var copiedSourceApkPath = Path.Combine(tempProcessedFolder, "source.apk");
            var apkToModifyPath = Path.Combine(tempProcessedFolder, "mod.apk");

            var containerZipPath = Path.Combine(GlobalVariables.PathToResources, "apk.zip");

            var resultFilePath = Path.Combine(Path.GetDirectoryName(apkFile) ?? string.Empty, Path.GetFileNameWithoutExtension(apkFile) + "_mod.apk");

            IOUtils.RecreateDir(tempProcessedFolder);

            File.Copy(apkFile, copiedSourceApkPath);

            var javaPath = GlobalVariables.PathToPortableJavaExe;
            if (!File.Exists(javaPath))
                javaPath = null;

            var apk = new Apktools(containerApkPath, GlobalVariables.PathToResources, javaExePath: javaPath);
            apk.Logging += Log;

            {
                string signed;
                apk.Sign(copiedSourceApkPath, out signed, deleteMetaInf: !_settings.AlternativeSigning);

                File.Move(signed, apkToModifyPath);
            }

            SetStep(MainResources.CopyingStgApk, 2);

            using (var zip = new ZipFile(containerZipPath)
            {
                Password = GlobalVariables.AdditionalFilePassword
            })
            {
                zip.ExtractAll(apk.FolderOfProject);
            }

            var mod = new ApkModifer.Logic.ApkModifer(apk);

            var backType = _settings.BackupType;

            mod.ProgressChanged += (o, args) =>
            {
                _visualProgress.SetBarValue((int)(args.Now * 100 / args.Maximum));
            };

            SetStep(MainResources.AddingLD, 3);

            if (!string.IsNullOrEmpty(saveFile) && File.Exists(saveFile))
            {
                _visualProgress.SetBarUsual();
                mod.AddLocalData(saveFile, backupType: backType);
            }

            SetStep(MainResources.AddingED, 4);

            if (!string.IsNullOrEmpty(androidDataFile) && File.Exists(androidDataFile))
            {
                _visualProgress.SetBarUsual();
                mod.AddExternalData(androidDataFile);
            }

            SetStep(MainResources.AddingObb, 5);

            if (androidObbFiles.Length > 0)
            {
                _visualProgress.SetBarUsual();
                mod.AddExternalObb(androidObbFiles);
            }

            _visualProgress.SetBarIndeterminate();

            SetStep(MainResources.CopyingApk, 6);

            File.Copy(apkToModifyPath, Path.Combine(apk.FolderOfProject, "assets", "install.bin"), true);

            string package;

            {
                string instMan = new Apktools(apkToModifyPath, GlobalVariables.PathToResources, javaExePath: javaPath).ExtractSimpleManifest();

                string temp = File.ReadAllText(instMan, Encoding.UTF8);

                const string packageStr = "package=\"";

                int startIndex = temp.IndexOf(packageStr, StringComparison.Ordinal) + packageStr.Length;
                int endIndex = temp.IndexOf('\"', startIndex);

                package = temp.Substring(startIndex, endIndex - startIndex);
            }

            File.WriteAllText(apk.PathToAndroidManifest,
                File.ReadAllText(apk.PathToAndroidManifest, Encoding.UTF8).Replace("change_package",
                    package).Replace("@string/app_name", appTitle));

            string iconsFolder = Path.Combine(apk.FolderOfProject, "res", "mipmap-");

            void DeleteIcon(string folder) =>
                IOUtils.DeleteFile(Path.Combine($"{iconsFolder}{folder}", "ic_launcher.png"));

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

            SetStep(MainResources.StepCompiling, 7);

            if (!apk.Compile(out _))
            {
                Log(MainResources.ErrorUp);
                return;
            }

            SetStep(MainResources.StepSigning, 8);

            apk.Sign(deleteMetaInf: !_settings.AlternativeSigning);

            SetStep(MainResources.MovingResult, 9);

            File.Copy(apk.SignedApk, resultFilePath, true);

            IOUtils.DeleteDir(tempProcessedFolder);

            _visualProgress.HideIndeterminateLabel();
            _visualProgress.HideBar();
            SetStep(MainResources.AllDone, 10);

            if (_settings.Notifications)
            {
                NotificationManager.Instance.Show(MainResources.Information_Title, MainResources.ModificationCompletedContent);
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
                SetIcon(dialog.FileName, e.OriginalSource.As<FrameworkElement>().Tag.As<string>());
        }

        private void SetStep(string step, int stepNumber)
        {
            WindowTitle.Value = step;
            Log(step);

            const int maxStep = 9;
            int percentage = (stepNumber - 1) * 100 / maxStep;

            _visualProgress.SetLabelText(step);
            _taskBarManager.SetProgress(percentage);
        }

        private void Log(string text)
        {
            _log.Append(text);
            _log.Append('\n');
            Dispatcher.InvokeAction(() =>
            {
                LogBox.Text = _log.ToString();
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
