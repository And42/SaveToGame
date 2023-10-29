using System.Diagnostics;
using Interfaces.ViewModels;
using MVVM_Tools.Code.Commands;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic.Utils;

namespace SaveToGameWpf.Logic.ViewModels
{
    public class AboutWindowViewModel : IAboutWindowViewModel
    {
        public IReadonlyProperty<string> Version { get; }

        public IActionCommand ShowDeveloperCommand { get; }
        public IActionCommand ThankDeveloperCommand { get; }
        public IActionCommand OpenAppDataFolderCommand { get; }
        public IActionCommand OpenSourcesPage { get; }

        public AboutWindowViewModel(
            ApplicationUtils applicationUtils,
            GlobalVariables globalVariables
        )
        {
            // properties
            Version = new DelegatedProperty<string>(applicationUtils.GetVersion, null).AsReadonly();

            // commands
            ShowDeveloperCommand = new ActionCommand(() =>
            {
                WebUtils.OpenLinkInBrowser("http://www.4pda.ru/forum/index.php?showuser=2114045");
            });
            ThankDeveloperCommand = new ActionCommand(() =>
            {
                WebUtils.OpenLinkInBrowser("http://4pda.ru/forum/index.php?act=rep&type=win_add&mid=2114045&p=23243303");
            });
            OpenAppDataFolderCommand = new ActionCommand(() =>
            {
                Process.Start(new ProcessStartInfo(globalVariables.AppDataPath)
                {
                    UseShellExecute = true
                });
            });
            OpenSourcesPage = new ActionCommand(() =>
            {
                WebUtils.OpenLinkInBrowser("https://github.com/And42/SaveToGame");
            });
        }
    }
}
