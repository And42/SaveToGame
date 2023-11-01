using System;
using System.Drawing;
using System.Windows.Forms;

namespace SaveToGameWpf.Logic.Classes
{
    public class NotificationManager : IDisposable
    {
        private const int DefaultTimeoutMs = 3000;

        private readonly NotifyIcon _trayIcon;

        public NotificationManager(
            GlobalVariables globalVariables
        )
        {
            _trayIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(globalVariables.PathToExe),
                Visible = true
            };
        }

        public void Show(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeoutMs = DefaultTimeoutMs)
        {
            _trayIcon.ShowBalloonTip(timeoutMs, title, text, icon);
        }

        public void Dispose()
        {
            _trayIcon.Visible = false;
        }
    }
}
