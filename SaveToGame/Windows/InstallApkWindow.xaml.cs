using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using Alphaleonis.Win32.Filesystem;
using AndroidLibs;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Logic.Interfaces;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Resources.Localizations;
using UsefulFunctionsLib;

using static SaveToGameWpf.Logic.GlobalVariables;

using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using Image = System.Windows.Controls.Image;
using IOHelper = UsefulFunctionsLib.UsefulFunctions_IOHelper;

namespace SaveToGameWpf.Windows
{
    /// <summary>
    /// Логика взаимодействия для InstallApkWindow.xaml
    /// </summary>
    public partial class InstallApkWindow : IRaisePropertyChanged
    {
        public AppIconsStorage IconsStorage { get; } = new AppIconsStorage();

        public string Apk
        {
            get => _apk;
            set
            {
                if (this.SetProperty(ref _apk, value))
                    AppTitle = Path.GetFileNameWithoutExtension(value) + " mod";
            }
        }
        private string _apk;

        public string Save
        {
            get => _save;
            set => this.SetProperty(ref _save, value);
        }
        private string _save;

        public string Data
        {
            get => _data;
            set => this.SetProperty(ref _data, value);
        }
        private string _data;

        public string[] Obb
        {
            get => _obb;
            set => this.SetProperty(ref _obb, value);
        }
        private string[] _obb;

        public bool Working
        {
            get => _working;
            private set => this.SetProperty(ref _working, value);
        }
        private bool _working;

        public string AppTitle
        {
            get => _appTitle;
            set => this.SetProperty(ref _appTitle, value);
        }
        private string _appTitle;

        public long ProgressNow
        {
            get => _progressNow;
            set => this.SetProperty(ref _progressNow, value);
        }
        private long _progressNow;

        public long ProgressMax
        {
            get => _progressMax;
            set => this.SetProperty(ref _progressMax, value);
        }   
        private long _progressMax;

        public string WindowTitle
        {
            get => _windowTitle;
            set => this.SetProperty(ref _windowTitle, value);
        }
        private string _windowTitle;

        public InstallApkWindow()
        {
            InitializeComponent();
            TaskbarItemInfo = new TaskbarItemInfo();

            var iconsFolder = Path.Combine(PathToResources, "icons");

            IconsStorage.Icon_xxhdpi_array = File.ReadAllBytes(Path.Combine(iconsFolder, "xxhdpi.png"));
            IconsStorage.Icon_xhdpi_array = File.ReadAllBytes(Path.Combine(iconsFolder, "xhdpi.png"));
            IconsStorage.Icon_hdpi_array = File.ReadAllBytes(Path.Combine(iconsFolder, "hdpi.png"));
            IconsStorage.Icon_mdpi_array = File.ReadAllBytes(Path.Combine(iconsFolder, "mdpi.png"));
        }

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
                Apk = openApkDialog.FileName;
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

            if (SettingsIncapsuler.BackupType != ApkModifer.ApkModifer.BackupType.LuckyPatcher)
            {
                if (openSaveDialog.ShowDialog() == true)
                    Save = openSaveDialog.FileName;
            }
            else
            {
                var (res, folderPath) = Utils.OpenFolderWithDialog();

                if (res)
                {
                    Save = folderPath;
                }
            }
        }

        private void ChooseDataClick(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = MainResources.ZipArchives + @" (*.zip)|*.zip",
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openDialog.ShowDialog() == true)
                Data = openDialog.FileName;
        }

        private void ChooseObbClick(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = MainResources.CacheFiles + @" (*.obb)|*.obb",
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = true
            };

            if (openDialog.ShowDialog() == true)
            {
                Obb = openDialog.FileNames;
            }
        }

        private void StartClick(object sender, RoutedEventArgs e)
        {
            if (Apk.NE() || !File.Exists(Apk))
                return;

            Working = true;
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

                    var containerZipPath = Path.Combine(PathToResources, "apk.zip");

                    var resultFilePath = Path.Combine(Path.GetDirectoryName(Apk) ?? string.Empty, Path.GetFileNameWithoutExtension(Apk) + "_mod.apk");

                    IOHelper.DeleteFolder(tempProcessedFolder);
                    Directory.CreateDirectory(tempProcessedFolder);

                    File.Copy(Apk, copiedSourceApkPath);

                    Apktools apk = new Apktools(containerApkPath, PathToResources);
                    apk.Logging += VisLog;

                    {
                        string signed;
                        apk.Sign(copiedSourceApkPath, out signed);

                        File.Move(signed, apkToModifyPath);
                    }

                    SetStep(MainResources.CopyingStgApk);

                    using (var zip = new ZipFile(containerZipPath)
                    {
                        Password = AdditionalFilePassword
                    })
                    {
                        zip.ExtractAll(apk.FolderOfProject);
                    }

                    ApkModifer.ApkModifer mod = new ApkModifer.ApkModifer(apk);

                    var backType = SettingsIncapsuler.BackupType;

                    mod.ProgressChanged += (o, args) =>
                    {
                        ProgressMax = args.Maximum;
                        ProgressNow = args.Now;
                    };

                    if (!Save.NE() && File.Exists(Save))
                    {
                        SetStep(MainResources.AddingLD);
                        mod.AddLocalData(Save, backupType: backType);
                    }

                    if (!Data.NE() && File.Exists(Data))
                    {
                        SetStep(MainResources.AddingED);
                        mod.AddExternalData(Data);
                    }

                    if (Obb?.Length > 0)
                    {
                        SetStep(MainResources.AddingObb);
                        mod.AddExternalObb(Obb);
                    }

                    SetStep(MainResources.CopyingApk);

                    File.Copy(apkToModifyPath, Path.Combine(apk.FolderOfProject, "assets", "install.bin"), true);

                    string package;

                    {
                        string instMan = new Apktools(apkToModifyPath, PathToResources).ExtractSimpleManifest();

                        string temp = File.ReadAllText(instMan, Encoding.UTF8);

                        const string packageStr = "package=\"";

                        int startIndex = temp.IndexOf(packageStr, StringComparison.Ordinal) + packageStr.Length;
                        int endIndex = temp.IndexOf('\"', startIndex);

                        package = temp.Substring(startIndex, endIndex - startIndex);
                    }

                    File.WriteAllText(apk.PathToAndroidManifest,
                        File.ReadAllText(apk.PathToAndroidManifest, Encoding.UTF8).Replace("change_package",
                            package).Replace("@string/app_name", AppTitle));

                    string iconsFolder = Path.Combine(apk.FolderOfProject, "res", "mipmap-");

                    IOHelper.DeleteFile(Path.Combine($"{iconsFolder}xxhdpi-v4", "ic_launcher.png"));
                    IOHelper.DeleteFile(Path.Combine($"{iconsFolder}xhdpi-v4", "ic_launcher.png"));
                    IOHelper.DeleteFile(Path.Combine($"{iconsFolder}hdpi-v4", "ic_launcher.png"));
                    IOHelper.DeleteFile(Path.Combine($"{iconsFolder}mdpi-v4", "ic_launcher.png"));

                    File.WriteAllBytes(Path.Combine($"{iconsFolder}xxhdpi-v4", "ic_launcher.png"), IconsStorage.Icon_xxhdpi_array);
                    File.WriteAllBytes(Path.Combine($"{iconsFolder}xhdpi-v4", "ic_launcher.png"), IconsStorage.Icon_xhdpi_array);
                    File.WriteAllBytes(Path.Combine($"{iconsFolder}hdpi-v4", "ic_launcher.png"), IconsStorage.Icon_hdpi_array);
                    File.WriteAllBytes(Path.Combine($"{iconsFolder}mdpi-v4", "ic_launcher.png"), IconsStorage.Icon_mdpi_array);
                    
                    SetStep(MainResources.Compiling);

                    if (!apk.Compile(out _))
                    {
                        VisLog(MainResources.ErrorUp);
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
                    VisLog($"{MainResources.ErrorUp}: {ex.Message}");
                }
            });
            tsk.ContinueWith(a =>
            {
                Working = false;
                Dispatcher.Invoke(new Action(() => TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None));
            });
            tsk.Start();
        }

        private void SetStep(string step)
        {
            WindowTitle = step;
            VisLog(step);
        }

        private readonly StringBuilder _log = new StringBuilder();

        private void VisLog(string text)
        {
            _log.Append(text);
            _log.Append('\n');
            Dispatcher.Invoke(new Action(() =>
            {
                LogBox.Text = _log.ToString();
            }));
        }

        private void Apk_DragOver(object sender, DragEventArgs e)
        {
            Utils.CheckDragOver(e, ".apk");
        }

        private void Apk_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files?.Length == 1 && Path.GetExtension(files[0]) == ".apk")
                Apk = files[0];

            e.Handled = true;
        }

        private void Save_DragOver(object sender, DragEventArgs e)
        {
            Utils.CheckDragOver(e, ".tar.gz");
        }

        private void Save_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files?.Length == 1 && files[0].EndsWith(".tar.gz", StringComparison.Ordinal))
                Save = files[0];

            e.Handled = true;
        }

        private void Data_DragOver(object sender, DragEventArgs e)
        {
            Utils.CheckDragOver(e, ".zip");
        }

        private void Data_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files?.Length == 1 && Path.GetExtension(files[0]) == ".zip")
                Data = files[0];

            e.Handled = true;
        }

        private void Obb_DragOver(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files == null)
                return;

            e.Effects = files.All(f => Path.GetExtension(f) == ".obb") ? DragDropEffects.Move : DragDropEffects.None;

            e.Handled = true;
        }

        private void Obb_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files == null)
                return;

            string[] obbs = files.Where(f => Path.GetExtension(f) == ".obb").ToArray();

            if (obbs.Length > 0)
                Obb = obbs;

            e.Handled = true;
        }

        private void SetIcon(string imagePath, string tag)
        {
            var img = new Bitmap(imagePath);

            switch (tag)
            {
                case "xxhdpi":
                    if (img.Height != img.Width || img.Height != 144)
                    {
                        img = img.Resize(144, 144);
                        IconsStorage.Icon_xxhdpi_array = img.ToByteArray();
                    }
                    else
                    {
                        IconsStorage.Icon_xxhdpi_array = File.ReadAllBytes(imagePath);
                    }
                    
                    break;
                case "xhdpi":
                    if (img.Height != img.Width || img.Height != 96)
                    {
                        img = img.Resize(96, 96);
                        IconsStorage.Icon_xhdpi_array = img.ToByteArray();
                    }
                    else
                    {
                        IconsStorage.Icon_xhdpi_array = File.ReadAllBytes(imagePath);
                    }

                    break;
                case "hdpi":
                    if (img.Height != img.Width || img.Height != 72)
                    {
                        img = img.Resize(72, 72);
                        IconsStorage.Icon_hdpi_array = img.ToByteArray();
                    }
                    else
                    {
                        IconsStorage.Icon_hdpi_array = File.ReadAllBytes(imagePath);
                    }

                    break;
                case "mdpi":
                    if (img.Height != img.Width || img.Height != 48)
                    {
                        img = img.Resize(48, 48);
                        IconsStorage.Icon_mdpi_array = img.ToByteArray();
                    }
                    else
                    {
                        IconsStorage.Icon_mdpi_array = File.ReadAllBytes(imagePath);
                    }

                    break;
            }
        }

        private void Icon_Drop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files == null || files.Length == 0)
                return;

            string imagePath = files[0];

            if (Path.GetExtension(imagePath) != ".png")
                return;

            string tag = e.OriginalSource.As<Image>().Tag.As<string>();

            SetIcon(imagePath, tag);
        }

        private void ChooseImage_Click(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = MainResources.Images + @" (*.png)|*.png"
            };

            if (dialog.ShowDialog() == true)
                SetIcon(dialog.FileName, e.OriginalSource.As<Image>().Tag.As<string>());
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
