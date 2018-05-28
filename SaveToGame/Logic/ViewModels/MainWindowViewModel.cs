using System.ComponentModel;
using System.Threading;
using ApkModifer.Logic;
using MVVM_Tools.Code.Classes;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic.OrganisationItems;
using SaveToGameWpf.Resources.Localizations;
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace SaveToGameWpf.Logic.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public Property<bool> Pro { get; private set; }

        public Property<bool> Working { get; private set; }
        public Property<bool> OnlySave { get; private set; }
        public Property<bool> SavePlusMess { get; private set; }
        public Property<bool> OnlyMess { get; private set; }

        public Property<string> PopupBoxText { get; private set; }
        public Property<int> MessagesCount { get; private set; }

        public Property<string> MainSmaliName { get; private set; }

        public Property<string> CurrentApk { get; private set; }
        public Property<string> CurrentSave { get; private set; }

        public Property<string> StatusLabel { get; private set; }

        public Property<int> StatusProgressNow { get; private set; }
        public Property<bool> StatusProgressIndeterminate { get; private set; }
        public Property<bool> StatusProgressVisible { get; private set; }

        public Property<bool> StatusProgressLabelVisible { get; private set; }

        public string Title => FormatTitle();

        public bool RuIsChecked => GetThreadLang().Contains("ru");
        public bool EnIsChecked => GetThreadLang().Contains("en");

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
            BindProperty(() => Pro);

            BindProperty(() => Working);
            BindProperty(() => OnlySave);
            BindProperty(() => SavePlusMess, true);
            BindProperty(() => OnlyMess);

            BindProperty(() => PopupBoxText, "Modified by SaveToGame");
            BindProperty(() => MessagesCount, 1);

            BindProperty(() => MainSmaliName, string.Empty);

            BindProperty(() => CurrentApk);
            BindProperty(() => CurrentSave);

            BindProperty(() => StatusLabel, MainResources.AllDone);

            BindProperty(() => StatusProgressNow);
            BindProperty(() => StatusProgressIndeterminate);
            BindProperty(() => StatusProgressVisible);

            BindProperty(() => StatusProgressLabelVisible);

            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Pro):
                case nameof(CurrentApk):
                    base.OnPropertyChanged(nameof(Title));
                    break;
            }
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

        private static string GetThreadLang()
        {
            return Thread.CurrentThread.CurrentCulture.ToString();
        }
    }
}
