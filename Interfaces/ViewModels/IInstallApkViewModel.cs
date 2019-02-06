using Interfaces.OrganisationItems;
using MVVM_Tools.Code.Commands;
using MVVM_Tools.Code.Providers;
using SharedData.Enums;

namespace Interfaces.ViewModels
{
    public interface IInstallApkViewModel
    {
        IAppIconsStorage IconsStorage { get; }

        Property<IVisualProgress> VisualProgress { get; }
        Property<ITaskBarManager> TaskBarManager { get; }

        Property<string> WindowTitle { get; }
        Property<bool> Working { get; }

        Property<string> Apk { get; }
        Property<string> Save { get; }
        Property<string> Data { get; }
        Property<string[]> Obb { get; }

        Property<string> AppTitle { get; }
        Property<string> LogText { get; }

        IActionCommand ChooseApkCommand { get; }
        IActionCommand ChooseSaveCommand { get; }
        IActionCommand ChooseDataCommand { get; }
        IActionCommand ChooseObbCommand { get; }
        IActionCommand StartCommand { get; }

        void SetIcon(string imagePath, AndroidAppIcon iconType);
    }
}
