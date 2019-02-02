using System.IO;
using SaveToGameWpf.Logic.Classes;
using SettingsManager;
using SettingsManager.ModelProcessors;

namespace SaveToGameWpf.Logic.OrganisationItems
{
    public class AppSettings : SettingsModel
    {
        public virtual string Language { get; set; } = "RU";

        public virtual BackupType BackupType { get; set; } = BackupType.Titanium;

        public virtual string PopupMessage { get; set; }

        public virtual string Theme { get; set; } = "Light";

        public virtual bool AlternativeSigning { get; set; }

        public virtual bool Notifications { get; set; } = true;
    }
}
