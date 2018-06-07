using ApkModifer.Logic;
using SaveToGameWpf.Properties;
using SettingsManager;

namespace SaveToGameWpf.Logic.OrganisationItems
{
    internal class DefaultSettingsContainer : SettingsContainerBase
    {
        public static DefaultSettingsContainer Instance { get; } = new DefaultSettingsContainer();

        private DefaultSettingsContainer() { }

        public string Language
        {
            get => GetValueInternal<string>();
            set => SetValueInternal(value);
        }

        public BackupType BackupType
        {
            get => GetValueInternal<BackupType>();
            set => SetValueInternal(value);
        }

        public string PopupMessage
        {
            get => GetValueInternal<string>();
            set => SetValueInternal(value);
        }

        public string Theme
        {
            get => GetValueInternal<string>();
            set => SetValueInternal(value);
        }

        public bool AlternativeSigning
        {
            get => GetValueInternal<bool>();
            set => SetValueInternal(value);
        }

        public bool Notifications
        {
            get => GetValueInternal<bool>();
            set => SetValueInternal(value);
        }

        public override void Save()
        {
            Settings.Default.Save();
        }

        protected override void SetSetting(string settingName, object value)
        {
            Settings.Default[settingName] = value;
        }

        protected override object GetSetting(string settingName)
        {
            return Settings.Default[settingName];
        }
    }
}
