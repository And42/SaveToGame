using System;
using System.Diagnostics;
using System.Net;

namespace SaveToGameWpf.Logic.Utils
{
    internal static class WebUtils
    {
        public static void DownloadStringAsync(Uri address, Action<DownloadStringCompletedEventArgs> onDownloaded)
        {
            var webClient = new WebClient();

            webClient.DownloadStringCompleted += (sender, args) =>
            {
                onDownloaded(args);

                webClient.Dispose();
            };

            webClient.DownloadStringAsync(address);
        }

        public static void OpenLinkInBrowser(string link)
        {
            try
            {
                Process.Start(link);
            }
            catch (Exception)
            {
                Process.Start(new ProcessStartInfo("IExplore.exe", link));
            }
        }
    }
}
