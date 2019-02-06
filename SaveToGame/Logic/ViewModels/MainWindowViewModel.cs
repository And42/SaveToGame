using System.Threading;
using Interfaces.OrganisationItems;
using JetBrains.Annotations;
using MVVM_Tools.Code.Classes;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Resources.Localizations;
using SharedData.Enums;

namespace SaveToGameWpf.Logic.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        [NotNull] public IAppSettings AppSettings { get; }
        
        public Property<bool> Working { get; }
        public Property<bool> OnlySave { get; }
        public Property<bool> SavePlusMess { get; }
        public Property<bool> OnlyMess { get; }

        public Property<string> PopupBoxText { get; }
        public Property<int> MessagesCount { get; }

        public Property<string> CurrentApk { get; }
        public Property<string> CurrentSave { get; }

        public string Title => FormatTitle();

        public bool RuIsChecked => !EnIsChecked;
        public bool EnIsChecked => Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.ToLower() == "en";

        public BackupType BackupType
        {
            get => AppSettings.BackupType;
            set
            {
                AppSettings.BackupType = value;
                base.OnPropertyChanged(nameof(BackupType));
            }
        }

        public MainWindowViewModel(
            [NotNull] IAppSettings appSettings
        )
        {
            AppSettings = appSettings;
            
            Working = new Property<bool>();
            OnlySave = new Property<bool>();
            SavePlusMess = new Property<bool>(true);
            OnlyMess = new Property<bool>();

            PopupBoxText = new Property<string>(appSettings.PopupMessage ?? "Modified by SaveToGame");
            MessagesCount = new Property<int>(1);

            CurrentApk = new Property<string>();
            CurrentSave = new Property<string>();

            CurrentApk.PropertyChanged += (sender, args) => OnPropertyChanged(nameof(CurrentApk));
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
