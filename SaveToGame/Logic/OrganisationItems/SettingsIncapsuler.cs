using ApkModifer.Logic;
using SaveToGameWpf.Properties;

namespace SaveToGameWpf.Logic.OrganisationItems
{
    public static class SettingsIncapsuler
    {
        public static string Language
        {
            get
            {
                if (!_isFieldLoadedLanguage)
                {
                    _language = Settings.Default.Language;
                    _isFieldLoadedLanguage = true;
                }

                return _language;
            }
            set
            {
                _language = value;
                Settings.Default.Language = value;
                Settings.Default.Save();
            }
        }
        private static string _language;
        private static bool _isFieldLoadedLanguage;

        public static BackupType BackupType
        {
            get
            {
                if (!_isFieldLoadedBackupType)
                {
                    _backupType = Settings.Default.BackupType;
                    _isFieldLoadedBackupType = true;
                }

                return _backupType;
            }
            set
            {
                _backupType = value;
                Settings.Default.BackupType = value;
                Settings.Default.Save();
            }
        }
        private static BackupType _backupType;
        private static bool _isFieldLoadedBackupType;

        public static string PopupMessage
        {
            get
            {
                if (!_isFieldLoadedPopupMessage)
                {
                    _popupMessage = Settings.Default.PopupMessage;
                    _isFieldLoadedPopupMessage = true;
                }

                return _popupMessage;
            }
            set
            {
                _popupMessage = value;
                Settings.Default.PopupMessage = value;
                Settings.Default.Save();
            }
        }
        private static string _popupMessage;
        private static bool _isFieldLoadedPopupMessage;
    }
}
