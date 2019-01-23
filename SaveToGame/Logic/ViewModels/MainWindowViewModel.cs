using System.Threading;
using ApkModifer.Logic;
using MVVM_Tools.Code.Classes;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Resources.Localizations;

namespace SaveToGameWpf.Logic.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public Property<bool> Pro { get; }

        public Property<bool> Working { get; }
        public Property<bool> OnlySave { get; }
        public Property<bool> SavePlusMess { get; }
        public Property<bool> OnlyMess { get; }

        public Property<string> PopupBoxText { get; }
        public Property<int> MessagesCount { get; }

        public Property<string> MainSmaliName { get; }

        public Property<string> CurrentApk { get; }
        public Property<string> CurrentSave { get; }

        public string Title => FormatTitle();

        public bool RuIsChecked => !EnIsChecked;
        public bool EnIsChecked => Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.ToLower() == "en";

        public BackupType BackupType
        {
            get => DefaultSettingsContainer.Instance.BackupType;
            set
            {
                DefaultSettingsContainer.Instance.BackupType = value;
                base.OnPropertyChanged(nameof(BackupType));
            }
        }

        public MainWindowViewModel()
        {
            Pro = new Property<bool>();

            Working = new Property<bool>();
            OnlySave = new Property<bool>();
            SavePlusMess = new Property<bool>(true);
            OnlyMess = new Property<bool>();

            PopupBoxText = new Property<string>("Modified by SaveToGame");
            MessagesCount = new Property<int>(1);

            MainSmaliName = new Property<string>(string.Empty);

            CurrentApk = new Property<string>();
            CurrentSave = new Property<string>();

            Pro.PropertyChanged += (sender, args) => OnPropertyChanged(nameof(Title));
            CurrentApk.PropertyChanged += (sender, args) => OnPropertyChanged(nameof(CurrentApk));
        }

        private string FormatTitle()
        {
            var result = MainResources.AppName;

            if (Pro.Value)
                result += " Pro";

            if (!string.IsNullOrEmpty(CurrentApk.Value))
                result += " - " + CurrentApk.Value;

            return result;
        }
    }
}
