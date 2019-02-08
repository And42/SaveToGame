using MVVM_Tools.Code.Providers;
using SharedData.Enums;

namespace Interfaces.ViewModels
{
    public interface IMainWindowViewModel
    {
        IProperty<bool> Working { get; }
        IProperty<bool> OnlySave { get; }
        IProperty<bool> SavePlusMess { get; }
        IProperty<bool> OnlyMess { get; }

        IProperty<string> PopupBoxText { get; }
        IProperty<int> MessagesCount { get; }

        IProperty<string> CurrentApk { get; }
        IProperty<string> CurrentSave { get; }

        IProperty<BackupType> BackupType { get; }
        IProperty<string> AppTheme { get; }
        IProperty<bool> AlternativeSigning { get; }
        IProperty<bool> NotificationsEnabled { get; }

        IReadonlyProperty<string> Title { get; }

        IReadonlyProperty<bool> RuIsChecked { get; }
        IReadonlyProperty<bool> EnIsChecked { get; }
    }
}
