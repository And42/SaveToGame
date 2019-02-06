using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MVVM_Tools.Code.Commands;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic.Utils;

namespace SaveToGameWpf.Logic.ViewModels
{
    public class AdbInstallWindowViewModel
    {
        [NotNull] private readonly GlobalVariables _globalVariables;

        private bool _dataLoaded;

        public ObservableCollection<AdbDeviceViewModel> Devices { get; } = new ObservableCollection<AdbDeviceViewModel>();

        public Property<bool> Processing { get; } = new Property<bool>();
        public Property<string> AdbLog { get; } = new Property<string>();

        public IActionCommand LoadDataCommand { get; }
        public IActionCommand<AdbDeviceViewModel> InstallCommand { get; }

        public AdbInstallWindowViewModel(
            [NotNull] GlobalVariables globalVariables
        )
        {
            _globalVariables = globalVariables;

            // commands
            LoadDataCommand = new ActionCommand(LoadData, () => !Processing.Value).BindCanExecute(Processing);
            InstallCommand = new ActionCommand<AdbDeviceViewModel>(InstallCommand_Execute, _ => !Processing.Value).BindCanExecute(Processing);
        }

        private async void InstallCommand_Execute(AdbDeviceViewModel device)
        {
            if (Processing.Value)
                return;

            Processing.Value = true;
            AdbLog.Value = string.Empty;

            var processInfo = new ProcessStartInfo(
                fileName: _globalVariables.AdbPath,
                arguments: $"-s {device.Id.Value} install \"{_globalVariables.LatestModdedApkPath}\""
            )
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var adbProcess = Process.Start(processInfo);

            object dataReceivedLock = new object();
            void OnDataReceived(object sender, DataReceivedEventArgs args)
            {
                lock (dataReceivedLock)
                {
                    AdbLog.Value += args.Data;
                }
            }

            adbProcess.OutputDataReceived += OnDataReceived;
            adbProcess.ErrorDataReceived += OnDataReceived;
            adbProcess.BeginOutputReadLine();
            adbProcess.BeginErrorReadLine();

            await Task.Run(() => adbProcess.WaitForExit());

            Processing.Value = false;
        }

        private async void LoadData()
        {
            if (Processing.Value)
                return;

            Processing.Value = true;

            var processInfo = new ProcessStartInfo(
                fileName: _globalVariables.AdbPath,
                arguments: "devices"
            )
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var adbProcess = Process.Start(processInfo);
            await Task.Run(() => adbProcess.WaitForExit());

            string output = adbProcess.StandardOutput.ReadToEnd();
            string[] deviceLines = output.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);

            AdbDeviceViewModel[] deviceModels = deviceLines.Skip(1).Select(line =>
            {
                string[] deviceParts = line.Split('\t');
                return new AdbDeviceViewModel
                {
                    Id = {Value = deviceParts[0]},
                    Title = {Value = deviceParts[1]}
                };
            }).ToArray();

            Devices.Clear();
            deviceModels.ForEach(Devices.Add);

            Processing.Value = false;
        }
    }
}
