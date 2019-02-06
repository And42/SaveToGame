using Interfaces.OrganisationItems;
using SettingsManager;
using SharedData.Enums;

namespace SaveToGameWpf.Logic.OrganisationItems
{
    public class AppSettings : SettingsModel, IAppSettings
    {
        public virtual string Language { get; set; } = "RU";

        public virtual BackupType BackupType { get; set; } = BackupType.Titanium;

        public virtual string PopupMessage { get; set; }

        public virtual string Theme { get; set; } = "Light";

        public virtual bool AlternativeSigning { get; set; }

        public virtual bool Notifications { get; set; } = true;

        public virtual bool SettingsMigrated { get; set; } = false;

        public virtual int Version { get; set; } = 0;
    }
}
