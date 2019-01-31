using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using SaveToGameWpf.Logic.JsonMappings;
using SaveToGameWpf.Logic.LongPaths;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Windows;

namespace SaveToGameWpf.Logic.Utils
{
    public static class ApplicationUtils
    {
        public static string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public static string GetPathToExe()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        public static void CheckForUpdate()
        {
            WebUtils.DownloadStringAsync(new Uri("https://storage.googleapis.com/savetogame/config.json"), args =>
            {
                if (args.Error != null)
                    return;

                WebConfig config;
                try
                {
                    config = JsonConvert.DeserializeObject<WebConfig>(args.Result);
                }
                catch (Exception ex)
                {
#if DEBUG
                    throw;
#endif

                    GlobalVariables.ErrorClient.Notify(ex);
                    return;
                }

                if (Utils.CompareVersions(GetVersion(), config.Version) >= 0)
                    return;

                string appLanguage = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.ToLower();

                string changesLink;

                try
                {
                    if (!config.ChangesLinks.TryGetValue(appLanguage, out changesLink))
                        changesLink = config.ChangesLinks["default"];
                }
                catch (Exception ex)
                {
#if DEBUG
                    throw;
#endif
                    GlobalVariables.ErrorClient.Notify(ex);
                    return;
                }

                WebUtils.DownloadStringAsync(new Uri(changesLink), ar =>
                {
                    if (ar.Error != null)
                        return;

                    new UpdateWindow(GetVersion(), ar.Result).ShowDialog();
                });
            });
        }

        public static bool GetIsPortable()
        {
            return LFile.Exists(Path.Combine(Path.GetDirectoryName(GetPathToExe()), "isportable"));
        }

        public static void SetLanguageFromSettings()
        {
            string lang = AppSettings.Instance.Language;

            if (string.IsNullOrEmpty(lang))
                return;

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(lang);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
        }
    }
}