using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using SaveToGameWpf.Logic;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Logic.LongPaths;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Properties;
using SaveToGameWpf.Resources.Localizations;
using SaveToGameWpf.Windows;

namespace SaveToGameWpf
{
    public partial class App
    {
        private static readonly string[] NeededFiles =
        {
            "apktool.jar",
            "baksmali.jar",
            "smali.jar",
            "signapk.jar",
            "testkey.pk8",
            "testkey.x509.pem"
        };

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!CheckForFiles(out string[] files))
                AppClose(files);

            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

#if !DEBUG
            DispatcherUnhandledException += (sender, args) =>
            {
                MessBox.ShowDial("Обнаружена непредвиденная ошибка. Текст ошибки скопирован в буфер обмена. Пожалуйста, свяжитесь с разработчиком");
                Clipboard.SetText("Message: " + args.Exception.Message + "\nStackTrace: " + args.Exception.StackTrace);
                args.Handled = true;

                GlobalVariables.ErrorClient.Notify(args.Exception);
            };
#endif

            ApplicationUtils.SetLanguageFromSettings();
            ThemeUtils.SetThemeFromSettings();

            WindowManager.ActivateWindow<MainWindow>();

#if !DEBUG
            if (!ApplicationUtils.GetIsPortable())
#endif
                ApplicationUtils.CheckForUpdate();
        }

        private static bool CheckForFiles(out string[] notExistingFiles)
        {
            var resourcesFolder = Path.Combine(GlobalVariables.PathToExeFolder, "Resources");

            if (!LDirectory.Exists(resourcesFolder))
            {
                notExistingFiles = new[] { resourcesFolder };
                return false;
            }

            notExistingFiles =
                NeededFiles.Select(it => Path.Combine(resourcesFolder, it))
                    .Where(it => !LFile.Exists(it)).ToArray();

            return notExistingFiles.Length == 0;
        }

        private void AppClose(IEnumerable<string> files)
        {
            MessBox.ShowDial(
                string.Format(
                    MainResources.NoNeededFilesError, 
                    files.Select(Path.GetFileName).Select(file => $"\"{file}\"").JoinStr(", ")
                ),
                MainResources.Error
            );

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            NotificationManager.Instance.Dispose();
        }
    }
}
