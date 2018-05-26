using System;
using System.ComponentModel;
using System.Net;
using Alphaleonis.Win32.Filesystem;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Resources.Localizations;
using UsefulFunctionsLib;

namespace SaveToGameWpf.Windows
{
    public partial class DownloadWindow
    {
        private const string UserAgent =
            "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Win64; x64; Trident/4.0; Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1) ; .NET CLR 2.0.50727; SLCC2; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; Tablet PC 2.0; .NET4.0C; .NET4.0E)";

        private static readonly Uri UpdateExeUri =
            new Uri("http://things.pixelcurves.info/Pages/Updates.aspx?cmd=stg_download");

        private static readonly string UpdateFilePath = 
            Path.Combine(Path.GetTempPath(), "STG Temp", "NewVersion.exe");

        public DownloadWindow()
        {
            InitializeComponent();
        }

        private bool _close;

        private void UpdateApplication()
        {
            try
            {
                var webClient = new WebClient
                {
                    Headers = { { "user-agent", UserAgent } }
                };

                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;

                var updateDir = Path.GetDirectoryName(UpdateFilePath);

                Directory.CreateDirectory(updateDir);

                webClient.DownloadFileAsync(UpdateExeUri, UpdateFilePath);
            }
            catch (Exception)
            {
                Utils.DeleteFile(UpdateFilePath);
            }
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            sender.As<WebClient>().Dispose();

            if (_close)
                return;

            try
            {
                Utils.RunAsAdmin(UpdateFilePath, string.Empty);
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
            ProcessBar.Value = e.ProgressPercentage;
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
