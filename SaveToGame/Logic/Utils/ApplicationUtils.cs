using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Interfaces.OrganisationItems;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SaveToGameWpf.Logic.JsonMappings;
using SaveToGameWpf.Windows;

namespace SaveToGameWpf.Logic.Utils
{
    public class ApplicationUtils
    {
        [NotNull] private readonly IAppSettings _appSettings;
        [NotNull] private readonly Provider<DownloadWindow> _downloadWindowProvider;
        [NotNull] private readonly GlobalVariables _globalVariables;

        public ApplicationUtils(
            [NotNull] IAppSettings appSettings,
            [NotNull] Provider<DownloadWindow> downloadWindowProvider,
            [NotNull] GlobalVariables globalVariables
        )
        {
            _appSettings = appSettings;
            _downloadWindowProvider = downloadWindowProvider;
            _globalVariables = globalVariables;
        }
        
        public string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public string GetPathToExe()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        public void CheckForUpdate()
        {
            WebUtils.DownloadStringAsync(new Uri("https://pixelcurves.ams3.digitaloceanspaces.com/SaveToGame/config.json"), args =>
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

                    _globalVariables.ErrorClient.Notify(ex);
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
                    _globalVariables.ErrorClient.Notify(ex);
                    return;
                }

                WebUtils.DownloadStringAsync(new Uri(changesLink), ar =>
                {
                    if (ar.Error != null)
                        return;

                    new UpdateWindow(
                        downloadWindowProvider: _downloadWindowProvider,
                        nowVersion: GetVersion(),
                        changes: ar.Result
                    ).ShowDialog();
                });
            });
        }

        public void SetLanguageFromSettings()
        {
            string lang = _appSettings.Language;

            if (string.IsNullOrEmpty(lang))
                return;

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(lang);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
        }
    }
}