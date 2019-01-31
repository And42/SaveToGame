using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic.LongPaths;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Resources.Localizations;

namespace SaveToGameWpf.Windows
{
    public partial class DownloadWindow
    {
        private static readonly Uri UpdateExeUri =
            new Uri("https://storage.googleapis.com/savetogame/latest_version_installer.exe");

        private static readonly string UpdateFilePath = 
            Path.Combine(Path.GetTempPath(), "STG Temp", "NewVersion.exe");

        public Property<int> ProgressNow { get; } = new Property<int>();

        public DownloadWindow()
        {
            InitializeComponent();
        }

        private bool _close;

        private void UpdateApplication()
        {
            try
            {
                var webClient = new WebClient();

                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;

                var updateDir = Path.GetDirectoryName(UpdateFilePath);

                LDirectory.CreateDirectory(updateDir);

                webClient.DownloadFileAsync(UpdateExeUri, UpdateFilePath);
            }
            catch (Exception)
            {
                LFile.Delete(UpdateFilePath);
            }
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            sender.As<WebClient>().Dispose();

            if (_close)
                return;

            try
            {
                Process.Start(UpdateFilePath);
                Environment.Exit(0);
            }
            catch (Exception)
            {
                MessBox.ShowDial(MainResources.Cant_update_program, MainResources.Error);
                Close();
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressNow.Value = e.ProgressPercentage;
        }

        private void DownloadCS_Load(object sender, EventArgs e)
        {
            UpdateApplication();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _close = true;
        }
    }
}
