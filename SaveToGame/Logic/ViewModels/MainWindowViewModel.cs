using System.Threading;
using Interfaces.OrganisationItems;
using Interfaces.ViewModels;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Resources.Localizations;
using SharedData.Enums;

namespace SaveToGameWpf.Logic.ViewModels
{
    public class MainWindowViewModel : IMainWindowViewModel
    {
        public IProperty<bool> Working { get; }
        public IProperty<bool> OnlySave { get; }
        public IProperty<bool> SavePlusMess { get; }
        public IProperty<bool> OnlyMess { get; }

        public IProperty<string> PopupBoxText { get; }
        public IProperty<int> MessagesCount { get; }

        public IProperty<string> CurrentApk { get; }
        public IProperty<string> CurrentSave { get; }

        public IProperty<BackupType> BackupType { get; }
        public IProperty<string> AppTheme { get; }
        public IProperty<bool> AlternativeSigning { get; }
        public IProperty<bool> NotificationsEnabled { get; }

        public IReadonlyProperty<string> Title { get; }

        public IReadonlyProperty<bool> RuIsChecked { get; }
        public IReadonlyProperty<bool> EnIsChecked { get; }

        public MainWindowViewModel(
            IAppSettings appSettings
        )
        {
            Working = new FieldProperty<bool>();
            OnlySave = new FieldProperty<bool>();
            SavePlusMess = new FieldProperty<bool>(true);
            OnlyMess = new FieldProperty<bool>();

            PopupBoxText = new FieldProperty<string>(appSettings.PopupMessage);
            MessagesCount = new FieldProperty<int>(1);

            CurrentApk = new FieldProperty<string>();
            CurrentSave = new FieldProperty<string>();

            BackupType = new DelegatedProperty<BackupType>(
                valueResolver: () => appSettings.BackupType,
                valueApplier: value => appSettings.BackupType = value
            ).DependsOn(appSettings, nameof(IAppSettings.BackupType));

            AppTheme = new DelegatedProperty<string>(
                valueResolver: () => appSettings.Theme,
                valueApplier: value => appSettings.Theme = value
            ).DependsOn(appSettings, nameof(IAppSettings.Theme));

            AlternativeSigning = new DelegatedProperty<bool>(
                valueResolver: () => appSettings.AlternativeSigning,
                valueApplier: value => appSettings.AlternativeSigning = value
            ).DependsOn(appSettings, nameof(IAppSettings.AlternativeSigning));

            NotificationsEnabled = new DelegatedProperty<bool>(
                valueResolver: () => appSettings.Notifications,
                valueApplier: value => appSettings.Notifications = value
            ).DependsOn(appSettings, nameof(IAppSettings.Notifications));

            Title = new DelegatedProperty<string>(
                valueResolver: FormatTitle,
                valueApplier: null
            ).DependsOn(CurrentApk).AsReadonly();
            
            EnIsChecked = new DelegatedProperty<bool>(
                valueResolver: () => Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.ToLower() == "en",
                valueApplier: null
            ).AsReadonly();

            RuIsChecked = new DelegatedProperty<bool>(
                valueResolver: () => !EnIsChecked.Value,
                valueApplier: null
            ).DependsOn(EnIsChecked).AsReadonly();
        }

        private string FormatTitle()
        {
            var result = MainResources.AppName;

            if (!string.IsNullOrEmpty(CurrentApk.Value))
                result += " - " + CurrentApk.Value;

            return result;
        }
    }
}
