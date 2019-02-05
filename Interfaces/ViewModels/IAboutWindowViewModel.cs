using MVVM_Tools.Code.Commands;
using MVVM_Tools.Code.Providers;

namespace Interfaces.ViewModels
{
    public interface IAboutWindowViewModel
    {
        Property<string> Version { get; }

        IActionCommand ShowDeveloperCommand { get; }
        IActionCommand ThankDeveloperCommand { get; }
        IActionCommand OpenAppDataFolderCommand { get; }
    }
}
