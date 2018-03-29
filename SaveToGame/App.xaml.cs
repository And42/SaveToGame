using System;
using System.Linq;
using System.Windows;
using Alphaleonis.Win32.Filesystem;
using SaveToGameWpf.Logic;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Properties;
using SaveToGameWpf.Windows;
using UsefulFunctionsLib;

namespace SaveToGameWpf
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App
    {
        private bool CheckForFiles(out string[] notExistingFiles)
        {
            notExistingFiles = new string[0];

            if (!Directory.Exists(Path.Combine(GlobalVariables.PathToExeFolder, "Resources")))
                return false;

            var list = new[] { /*"aapt.exe",*/ "apktool.jar", "baksmali.jar", "smali.jar", "signapk.jar", "testkey.pk8", "testkey.x509.pem" };
            notExistingFiles = list.Select(it => Path.Combine(GlobalVariables.PathToExeFolder, "Resources", it)).Where(it => !File.Exists(it)).ToArray();
            return notExistingFiles.Length == 0;
        }

        private void AppClose(string[] files)
        {
            MessageBox.Show("Нет необходимых файлов для корректной работы программы!\nОтсутствующие файлы: " + files.Select(Path.GetFileName).Select(file => $"\"{file}\"").JoinStr(", "), "Ошибка!");
            Environment.Exit(0);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
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
            };
#endif

            WindowManager.ActivateWindow<MainWindow>();
        }
    }
}
