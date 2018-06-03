using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Properties;
using SaveToGameWpf.Windows;

namespace SaveToGameWpf.Logic.Utils
{
    public static class ApplicationUtils
    {
        private static readonly Dictionary<string, string> AppLanguageToChangesLinkDict = new Dictionary<string, string>
        {
            { "en", "https://storage.googleapis.com/savetogame/changes_en.xml" }
        };

        public static string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
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
                catch (Exception ex)
                {
                    GlobalVariables.ErrorClient.Notify(ex);
                }
            });
        }

        public static string GetPathToExe()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        public static void CheckForUpdate()
        {
            var webClient = new WebClient();

            webClient.DownloadStringCompleted += (sender, args) =>
            {
                if (args.Error != null)
                    return;

                string newVersion = args.Result;

                if (Utils.CompareVersions(GetVersion(), newVersion) >= 0)
                    return;

                string appLanguage = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.ToLower();

                string changesLink;
                if (!AppLanguageToChangesLinkDict.TryGetValue(appLanguage, out changesLink))
                    changesLink = "https://storage.googleapis.com/savetogame/changes_ru.xml";

                string changes = webClient.DownloadString(changesLink);

                new UpdateWindow(GetVersion(), changes).ShowDialog();

                webClient.Dispose();
            };

            webClient.DownloadStringAsync(new Uri("https://storage.googleapis.com/savetogame/latest_version.txt"));
        }

        public static bool GetIsPortable()
        {
            return File.Exists(Path.Combine(Path.GetDirectoryName(GetPathToExe()), "isportable"));
        }

        public static void SetLanguageFromSettings()
        {
            string lang = DefaultSettingsContainer.Instance.Language;

            if (string.IsNullOrEmpty(lang))
                return;

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(lang);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
        }
    }
}