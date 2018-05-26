using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Alphaleonis.Win32.Filesystem;
using AndroidHelper.Logic;
using ApkModifer.Logic;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Resources.Localizations;
using UsefulFunctionsLib;

using DragEventArgs = System.Windows.DragEventArgs;
using Image = System.Windows.Controls.Image;
using IOHelper = UsefulFunctionsLib.UsefulFunctions_IOHelper;

namespace SaveToGameWpf.Windows
{
    public partial class InstallApkWindow : IRaisePropertyChanged
    {
        public AppIconsStorage IconsStorage { get; }

        public Property<string> WindowTitle { get; } = new Property<string>();
        public Property<bool> Working { get; } = new Property<bool>();

        public Property<string> Apk { get; } = new Property<string>();
        public Property<string> Save { get; } = new Property<string>();
        public Property<string> Data { get; } = new Property<string>();
        public Property<string[]> Obb { get; } = new Property<string[]>();

        public Property<string> AppTitle { get; } = new Property<string>();

        public Property<long> ProgressNow { get; } = new Property<long>();
        public Property<long> ProgressMax { get; } = new Property<long>();

        private readonly DefaultSettingsContainer _settings = DefaultSettingsContainer.Instance;

        private readonly StringBuilder _log = new StringBuilder();

        public InstallApkWindow()
        {
            InitializeComponent();
            TaskbarItemInfo = new TaskbarItemInfo();

            var iconsFolder = Path.Combine(GlobalVariables.PathToResources, "icons");

            BitmapSource GetImage(string name) =>
                File.ReadAllBytes(Path.Combine(iconsFolder, name)).ToBitmap().ToBitmapSource();

            IconsStorage = new AppIconsStorage
            {
                Icon_xxhdpi = {Value = GetImage("xxhdpi.png")},
                Icon_xhdpi = {Value = GetImage("xhdpi.png")},
                Icon_hdpi = {Value = GetImage("hdpi.png")},
                Icon_mdpi = {Value = GetImage("mdpi.png")}
            };

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

        private void StartClick(object sender, RoutedEventArgs e)
        {
            string apkFile = Apk.Value;
            string saveFile = Save.Value;
            string androidDataFile = Data.Value;
            string[] androidObbFiles = Obb.Value?.CloneTyped() ?? new string[0];
            string appTitle = AppTitle.Value;

            if (string.IsNullOrEmpty(apkFile) || !File.Exists(apkFile))
                return;

            Working.Value = true;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;

            _log.Clear();
            LogBox.Text = "";

            Task tsk = new Task(() =>
            {  
                try
                {
                    SetStep(MainResources.Preparing);

                    var tempProcessedFolder = Path.Combine(Path.GetTempPath(), "STG_temp");

                    var containerApkPath = Path.Combine(tempProcessedFolder, "apk.apk");
                    var copiedSourceApkPath = Path.Combine(tempProcessedFolder, "source.apk");
                    var apkToModifyPath = Path.Combine(tempProcessedFolder, "mod.apk");

                    var containerZipPath = Path.Combine(GlobalVariables.PathToResources, "apk.zip");

                    var resultFilePath = Path.Combine(Path.GetDirectoryName(apkFile) ?? string.Empty, Path.GetFileNameWithoutExtension(apkFile) + "_mod.apk");

                    IOHelper.DeleteFolder(tempProcessedFolder);
                    Directory.CreateDirectory(tempProcessedFolder);

                    File.Copy(apkFile, copiedSourceApkPath);

                    Apktools apk = new Apktools(containerApkPath, GlobalVariables.PathToResources);
                    apk.Logging += Log;

                    {
                        string signed;
                        apk.Sign(copiedSourceApkPath, out signed);

                        File.Move(signed, apkToModifyPath);
                    }

                    SetStep(MainResources.CopyingStgApk);

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
                        ProgressMax.Value = args.Maximum;
                        ProgressNow.Value = args.Now;
                    };

                    if (!string.IsNullOrEmpty(saveFile) && File.Exists(saveFile))
                    {
                        SetStep(MainResources.AddingLD);
                        mod.AddLocalData(saveFile, backupType: backType);
                    }

                    if (!string.IsNullOrEmpty(androidDataFile) && File.Exists(androidDataFile))
                    {
                        SetStep(MainResources.AddingED);
                        mod.AddExternalData(androidDataFile);
                    }

                    if (androidObbFiles.Length > 0)
                    {
                        SetStep(MainResources.AddingObb);
                        mod.AddExternalObb(androidObbFiles);
                    }

                    SetStep(MainResources.CopyingApk);

                    File.Copy(apkToModifyPath, Path.Combine(apk.FolderOfProject, "assets", "install.bin"), true);

                    string package;

                    {
                        string instMan = new Apktools(apkToModifyPath, GlobalVariables.PathToResources).ExtractSimpleManifest();

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
                        IOHelper.DeleteFile(Path.Combine($"{iconsFolder}{folder}", "ic_launcher.png"));

                    DeleteIcon("xxhdpi-v4");
                    DeleteIcon("xhdpi-v4");
                    DeleteIcon("hdpi-v4");
                    DeleteIcon("mdpi-v4");

                    void WriteIcon(string folder, Property<BitmapSource> property) =>
                        File.WriteAllBytes(Path.Combine($"{iconsFolder}{folder}", "ic_launcher.png"), property.Value.ToBitmap().ToByteArray());

                    WriteIcon("xxhdpi-v4", IconsStorage.Icon_xxhdpi);
                    WriteIcon("xhdpi-v4", IconsStorage.Icon_xhdpi);
                    WriteIcon("hdpi-v4", IconsStorage.Icon_hdpi);
                    WriteIcon("mdpi-v4", IconsStorage.Icon_mdpi);

                    SetStep(MainResources.Compiling);

                    if (!apk.Compile(out _))
                    {
                        Log(MainResources.ErrorUp);
                        return;
                    }

                    SetStep(MainResources.Signing);

                    apk.Sign();

                    SetStep(MainResources.MovingResult);

                    File.Copy(apk.SignedApk, resultFilePath, true);

                    IOHelper.DeleteFolder(tempProcessedFolder);

                    SetStep(MainResources.AllDone);

                    if (MessBox.ShowDial(
                            Properties.Resources.Path_to_file + resultFilePath,
                            Properties.Resources.Successful,
                            Properties.Resources.OK, MainResources.Open
                        ) == MainResources.Open)
                    {
                        Process.Start("explorer.exe", $"/select,{resultFilePath}");
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Trace.WriteLine(ex.ToString());
                    Dispatcher.InvokeAction(() => throw ex);
#endif
                    Log($"{MainResources.ErrorUp}: {ex.Message}");
                }
            });
            tsk.ContinueWith(a =>
            {
                Working.Value = false;
                Dispatcher.Invoke(new Action(() => TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None));
            });
            tsk.Start();
        }

        #endregion

        #region Drag & Drop

        private void Apk_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".apk");
        }

        private void Apk_DragDrop(object sender, DragEventArgs e)
        {
            DropOneByEnd(e, ".apk", file => Apk.Value = file);
        }

        private void Save_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".tar.gz");
        }

        private void Save_DragDrop(object sender, DragEventArgs e)
        {
            DropOneByEnd(e, ".tar.gz", file => Save.Value = file);
        }

        private void Data_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".zip");
        }

        private void Data_DragDrop(object sender, DragEventArgs e)
        {
            DropOneByEnd(e, ".zip", file => Data.Value = file);
        }

        private void Obb_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".obb");
        }

        private void Obb_DragDrop(object sender, DragEventArgs e)
        {
            DropManyByEnd(e, ".obb", files => Obb.Value = files);
        }

        private void Icon_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".png");
        }

        private void Icon_Drop(object sender, DragEventArgs e)
        {
            DropOneByEnd(e, ".png", file =>
            {
                string tag = e.OriginalSource.As<Image>().Tag.As<string>();
                SetIcon(file, tag);
            });
        }

        #endregion

        private static void DropOneByEnd(DragEventArgs e, string ext, Action<string> onSuccess)
        {
            string[] files = e.GetFilesDrop(ext);

            if (files.Length == 1)
                onSuccess(files[0]);

            e.Handled = true;
        }

        private static void DropManyByEnd(DragEventArgs e, string ext, Action<string[]> onSuccess)
        {
            string[] files = e.GetFilesDrop(ext);

            if (files.Length > 0)
                onSuccess(files);

            e.Handled = true;
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

        private void SetStep(string step)
        {
            WindowTitle.Value = step;
            Log(step);
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

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
