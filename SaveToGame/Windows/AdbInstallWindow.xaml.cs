using System.Windows;
using SaveToGameWpf.Logic.ViewModels;

namespace SaveToGameWpf.Windows
{
    public partial class AdbInstallWindow
    {
        private AdbInstallWindowViewModel ViewModel { get; }

        public AdbInstallWindow(
            AdbInstallWindowViewModel viewModel
        )
        {
            ViewModel = viewModel;

            InitializeComponent();
            DataContext = viewModel;

            // property change notifications
            viewModel.AdbLog.PropertyChanged += (sender, args) => AdbLogBox.Dispatcher.Invoke(AdbLogBox.ScrollToEnd);
        }

        #region Window events

        private void AdbInstallWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadDataCommand.Execute();
        }

        #endregion
    }
}
