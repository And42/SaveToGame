using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Alphaleonis.Win32.Filesystem;
using SaveToGameWpf.Properties;
using SaveToGameWpf.Windows;

namespace SaveToGameWpf.Logic.Utils
{
    public static class ApplicationUtils
    {
        private static readonly Dictionary<string, string> AppLanguageToChangesLinkDict = new Dictionary<string, string>
        {
            { "en", "http://things.pixelcurves.info/Pages/Updates.aspx?cmd=stg_changes&language=en" }
        };

        public static string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            return assembly.GetName().Version.ToString();
        }

        public static void LoadSettings()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (LicensingUtils.IsLicenseValid(Settings.Default.License))
                        Utils.ProVersionEnable(true);
                }
                catch (Exception)
                {
                    // ignored
                }
            });
        }

        public static bool GetIsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            var p = new WindowsPrincipal(id);

            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string GetPathToExe()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        public static void CheckForUpdate()
        {
            Task.Factory.StartNew(() =>
            {
                using (var webClient = new WebClient
                {
                    Headers = { { "user-agent", "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Win64; x64; Trident/4.0; Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1) ; .NET CLR 2.0.50727; SLCC2; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; Tablet PC 2.0; .NET4.0C; .NET4.0E)" } }
                })
                {
                    string newVersion;

                    try
                    {
                        newVersion = webClient.DownloadString("http://things.pixelcurves.info/Pages/Updates.aspx?cmd=stg_version");
                    }
                    catch (WebException)
                    {
                        return;
                    }

                    if (Utils.CompareVersions(GetVersion(), newVersion) >= 0)
                        return;

                    string appLanguage = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.ToLower();

                    string changesLink;
                    if (!AppLanguageToChangesLinkDict.TryGetValue(appLanguage, out changesLink))
                        changesLink = "http://things.pixelcurves.info/Pages/Updates.aspx?cmd=stg_changes";

                    string changes = webClient.DownloadString(changesLink);

                    Application.Current.Dispatcher.InvokeAction(() =>
                    {
                        new UpdateWindow(GetVersion(), changes).ShowDialog();
                    });
                }
            });
        }

        public static bool GetIsPortable()
        {
            return File.Exists(Path.Combine(Path.GetDirectoryName(GetPathToExe()), "isportable"));
        }
    }
}