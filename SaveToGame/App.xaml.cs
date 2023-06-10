using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using AndroidHelper.Logic;
using AndroidHelper.Logic.Interfaces;
using Autofac;
using Interfaces.OrganisationItems;
using Interfaces.ViewModels;
using SaveToGameWpf.Logic;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Logic.ViewModels;
using SaveToGameWpf.Resources.Localizations;
using SaveToGameWpf.Windows;
using SettingsManager;
using SettingsManager.ModelProcessors;

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

        [JetBrains.Annotations.NotNull]
        private IContainer _rootDiContainer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _rootDiContainer = SetupDI();
            
            MigrateSettings(_rootDiContainer.Resolve<IAppSettings>());
            var globalVariables = _rootDiContainer.Resolve<GlobalVariables>();
            CheckForFiles(globalVariables);
            CheckPortability(globalVariables);

#if !DEBUG
            DispatcherUnhandledException += (sender, args) =>
            {
                MessBox.ShowDial("Обнаружена непредвиденная ошибка. Текст ошибки скопирован в буфер обмена. Пожалуйста, свяжитесь с разработчиком");
                Clipboard.SetText("Message: " + args.Exception.Message + "\nStackTrace: " + args.Exception.StackTrace);
                args.Handled = true;

                globalVariables.ErrorClient.Notify(args.Exception);
            };
#endif

            var applicationUtils = _rootDiContainer.Resolve<ApplicationUtils>();

            applicationUtils.SetLanguageFromSettings();
            _rootDiContainer.Resolve<ThemeUtils>().SetThemeFromSettings();

            _rootDiContainer.Resolve<MainWindow>().Show();

#if !DEBUG
            if (!globalVariables.IsPortable)
#endif
                applicationUtils.CheckForUpdate();
        }

        private void CheckPortability([JetBrains.Annotations.NotNull] GlobalVariables globalVariables)
        {
            if (!globalVariables.IsPortable || globalVariables.CanWriteToAppData.Value)
                return;

            MessBox.ShowDial(
                string.Format(
                    MainResources.DataWriteDenied,
                    globalVariables.AppDataPath,
                    globalVariables.PortableSwitchFile
                ),
                MainResources.Error
            );

            Shutdown();
        }

        private void CheckForFiles([JetBrains.Annotations.NotNull] GlobalVariables globalVariables)
        {
            var resourcesFolder = Path.Combine(globalVariables.PathToExeFolder, "Resources");

            string[] notExistingFiles;
            if (!Directory.Exists(resourcesFolder))
            {
                notExistingFiles = new[] { resourcesFolder };
            }
            else
            {
                notExistingFiles =
                    NeededFiles.Select(it => Path.Combine(resourcesFolder, it))
                        .Where(it => !File.Exists(it)).ToArray();
            }

            if (notExistingFiles.Length == 0)
                return;

            MessBox.ShowDial(
                string.Format(
                    MainResources.NoNeededFilesError,
                    notExistingFiles.Select(Path.GetFileName).Select(file => $"\"{file}\"").JoinStr(", ")
                ),
                MainResources.Error
            );

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            _rootDiContainer.Resolve<NotificationManager>().Dispose();
        }

        // ReSharper disable once InconsistentNaming
        [JetBrains.Annotations.NotNull]
        [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
        private IContainer SetupDI()
        {
            var builder = new ContainerBuilder();

            builder.RegisterGeneric(typeof(Provider<>)).SingleInstance();
            
            // custom construction
            builder.Register<AppSettings>(c =>
            {
                // ReSharper disable once ConvertToLambdaExpression
                return new SettingsBuilder<AppSettings>()
                    .WithFile(
                        Path.Combine(
                            c.Resolve<GlobalVariables>().AppDataPath,
                            "appSettings.json"
                        )
                    )
                    .WithProcessor(new JsonModelProcessor())
                    .Build();
            }).As<IAppSettings>().SingleInstance();
            builder.Register(c =>
            {
                var globalVariables = c.Resolve<GlobalVariables>();

                Apktool apktool = new Apktool.Builder()
                    .JavaPath(globalVariables.PathToPortableJavaExe)
                    .ApktoolPath(globalVariables.ApktoolPath)
                    .SignApkPath(globalVariables.SignApkPath)
                    .BaksmaliPath(globalVariables.BaksmaliPath)
                    .SmaliPath(globalVariables.SmaliPath)
                    .DefaultKeyPemPath(globalVariables.DefaultKeyPemPath)
                    .DefaultKeyPkPath(globalVariables.DefaultKeyPkPath)
                    .Build();

                var modified = new ApkSignerApktool(
                    apktool: apktool,
                    apkSignerPath: globalVariables.ApkSignerPath,
                    zipalignPath: globalVariables.ZipalignPath,
                    aapt2Path: globalVariables.Aapt2Path
                );

                return modified;
            }).As<IApktool, IApktoolExtra>().SingleInstance();

            // basic construction
            builder.RegisterType<ApplicationUtils>().SingleInstance();
            builder.RegisterType<ThemeUtils>().SingleInstance();
            builder.RegisterType<TempUtils>().SingleInstance();
            builder.RegisterType<NotificationManager>().SingleInstance();
            builder.RegisterType<GlobalVariables>().SingleInstance();
            builder.RegisterType<Utils>().SingleInstance();
            
            // windows
            builder.RegisterType<MainWindow>();
            builder.RegisterType<InstallApkWindow>();
            builder.RegisterType<AboutWindow>();
            builder.RegisterType<UpdateWindow>();
            builder.RegisterType<DownloadWindow>();
            builder.RegisterType<AdbInstallWindow>();
            
            // window models
            builder.RegisterType<MainWindowViewModel>().As<IMainWindowViewModel>();
            builder.RegisterType<InstallApkViewModel>().As<IInstallApkViewModel>();
            builder.RegisterType<AboutWindowViewModel>().As<IAboutWindowViewModel>();
            builder.RegisterType<AdbInstallWindowViewModel>();
            
            return builder.Build();
        }

        private static void MigrateSettings([JetBrains.Annotations.NotNull] IAppSettings latestSettings)
        {
            int currentVersion = latestSettings.Version;
            if (currentVersion == GlobalVariables.LatestSettingsVersion)
                return;

            if (currentVersion < 0)
                currentVersion = 0;

            while (currentVersion < GlobalVariables.LatestSettingsVersion)
            {
                switch (currentVersion)
                {
                    case 0:
                        currentVersion = 1;
                        break;
                }
            }

            if (currentVersion > GlobalVariables.LatestSettingsVersion)
                currentVersion = GlobalVariables.LatestSettingsVersion;

            latestSettings.Version = currentVersion;
        }
    }
}
