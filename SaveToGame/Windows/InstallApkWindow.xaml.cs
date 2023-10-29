using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using Interfaces.OrganisationItems;
using Interfaces.ViewModels;
using Microsoft.Win32;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Resources.Localizations;
using SharedData.Enums;
using DragEventArgs = System.Windows.DragEventArgs;

namespace SaveToGameWpf.Windows
{
    public partial class InstallApkWindow
    {
        private readonly IInstallApkViewModel _viewModel;

        public InstallApkWindow(
            IInstallApkViewModel viewModel
        )
        {
            _viewModel = viewModel;

            InitializeComponent();
            DataContext = viewModel;

            ITaskBarManager taskBarManager = new TaskBarManager(TaskbarItemInfo = new TaskbarItemInfo());
            IVisualProgress visualProgress = StatusProgress.GetVisualProgress();

            viewModel.TaskBarManager.Value = taskBarManager;
            viewModel.VisualProgress.Value = visualProgress;

            visualProgress.SetLabelText(MainResources.AllDone);

            // property changes notification
            viewModel.LogText.PropertyChanged += (sender, args) => LogBox.Dispatcher.Invoke(() => LogBox.ScrollToEnd());
        }

        #region Drag & Drop

        private void Apk_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".apk");
        }

        private void Apk_DragDrop(object sender, DragEventArgs e)
        {
            e.DropOneByEnd(".apk", file => _viewModel.Apk.Value = file);
        }

        private void Save_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".tar.gz");
        }

        private void Save_DragDrop(object sender, DragEventArgs e)
        {
            e.DropOneByEnd(".tar.gz", file => _viewModel.Save.Value = file);
        }

        private void Data_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".zip");
        }

        private void Data_DragDrop(object sender, DragEventArgs e)
        {
            e.DropOneByEnd(".zip", file => _viewModel.Data.Value = file);
        }

        private void Obb_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".obb");
        }

        private void Obb_DragDrop(object sender, DragEventArgs e)
        {
            e.DropManyByEnd(".obb", files => _viewModel.Obb.Value = files);
        }

        private void Icon_DragOver(object sender, DragEventArgs e)
        {
            e.CheckDragOver(".png");
        }

        private void Icon_Drop(object sender, DragEventArgs e)
        {
            e.DropOneByEnd(".png", file =>
            {
                var tag = sender.As<FrameworkElement>().Tag.As<AndroidAppIcon>();
                _viewModel.SetIcon(file, tag);
            });
        }

        #endregion

        private void ChooseImage_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = MainResources.Images + @" (*.png)|*.png"
            };

            if (dialog.ShowDialog() == true)
                _viewModel.SetIcon(dialog.FileName, sender.As<FrameworkElement>().Tag.As<AndroidAppIcon>());
        }
    }
}
