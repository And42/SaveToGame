using System.Diagnostics;
using Interfaces.ViewModels;
using JetBrains.Annotations;
using MVVM_Tools.Code.Commands;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic.Utils;

namespace SaveToGameWpf.Logic.ViewModels
{
    public class AboutWindowViewModel : IAboutWindowViewModel
    {
        public Property<string> Version { get; }

        public IActionCommand ShowDeveloperCommand { get; }
        public IActionCommand ThankDeveloperCommand { get; }
        public IActionCommand OpenAppDataFolderCommand { get; }

        public AboutWindowViewModel(
            [NotNull] ApplicationUtils applicationUtils,
            [NotNull] GlobalVariables globalVariables
        )
        {
            // properties
            Version = new Property<string>(applicationUtils.GetVersion());

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
                Process.Start(globalVariables.AppDataPath);
            });
        }
    }
}
