using Interfaces.OrganisationItems;
using MVVM_Tools.Code.Commands;
using MVVM_Tools.Code.Providers;
using SharedData.Enums;

namespace Interfaces.ViewModels
{
    public interface IInstallApkViewModel
    {
        IAppIconsStorage IconsStorage { get; }

        IProperty<IVisualProgress> VisualProgress { get; }
        IProperty<ITaskBarManager> TaskBarManager { get; }

        IProperty<string> WindowTitle { get; }
        IProperty<bool> Working { get; }

        IProperty<string> Apk { get; }
        IProperty<string> Save { get; }
        IProperty<string> Data { get; }
        IProperty<string[]> Obb { get; }

        IReadonlyProperty<string> AppTitle { get; }
        IProperty<string> LogText { get; }

        IActionCommand ChooseApkCommand { get; }
        IActionCommand ChooseSaveCommand { get; }
        IActionCommand ChooseDataCommand { get; }
        IActionCommand ChooseObbCommand { get; }
        IActionCommand StartCommand { get; }

        void SetIcon(string imagePath, AndroidAppIcon iconType);
    }
}
