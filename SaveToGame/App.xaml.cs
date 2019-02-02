using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using AndroidHelper.Logic;
using AndroidHelper.Logic.Interfaces;
using Autofac;
using Interfaces.OrganisationItems;
using JetBrains.Annotations;
using LongPaths.Logic;
using SaveToGameWpf.Logic;
using SaveToGameWpf.Logic.Classes;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Logic.ViewModels;
using SaveToGameWpf.Properties;
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

        [NotNull]
        private IContainer _rootDiContainer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _rootDiContainer = SetupDI();
            
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

            var applicationUtils = _rootDiContainer.Resolve<ApplicationUtils>();

            applicationUtils.SetLanguageFromSettings();
            _rootDiContainer.Resolve<ThemeUtils>().SetThemeFromSettings();

            _rootDiContainer.Resolve<MainWindow>().Show();

#if !DEBUG
            if (!ApplicationUtils.GetIsPortable())
#endif
                applicationUtils.CheckForUpdate();
        }

        private bool CheckForFiles(out string[] notExistingFiles)
        {
            var resourcesFolder = Path.Combine(_rootDiContainer.Resolve<GlobalVariables>().PathToExeFolder, "Resources");

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

            _rootDiContainer.Resolve<NotificationManager>().Dispose();
        }

        // ReSharper disable once InconsistentNaming
        [NotNull]
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
            builder.Register<Apktool>(c =>
            {
                var globalVariables = c.Resolve<GlobalVariables>();

                return new Apktool.Builder()
                    .JavaPath(globalVariables.PathToPortableJavaExe)
                    .ApktoolPath(globalVariables.ApktoolPath)
                    .SignApkPath(globalVariables.SignApkPath)
                    .BaksmaliPath(globalVariables.BaksmaliPath)
                    .SmaliPath(globalVariables.SmaliPath)
                    .DefaultKeyPemPath(globalVariables.DefaultKeyPemPath)
                    .DefaultKeyPkPath(globalVariables.DefaultKeyPkPath)
                    .Build();
            }).As<IApktool>().SingleInstance();

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
            
            // window models
            builder.RegisterType<MainWindowViewModel>();
            builder.RegisterType<InstallApkViewModel>();
            
            return builder.Build();
        }
    }
}
