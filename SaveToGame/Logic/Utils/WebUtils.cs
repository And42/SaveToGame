using System;
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
    }
}
