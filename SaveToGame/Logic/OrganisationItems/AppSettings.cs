using System.IO;
using ApkModifer.Logic;
using SettingsManager;
using SettingsManager.ModelProcessors;

namespace SaveToGameWpf.Logic.OrganisationItems
{
    public class AppSettings : SettingsModel
    {
        public static AppSettings Instance { get; }

        static AppSettings()
        {
            Instance = new SettingsBuilder<AppSettings>()
                .WithFile(
                    Path.Combine(
                        GlobalVariables.AppDataPath,
                        "appSettings.json"
                    )
                )
                .WithProcessor(new JsonModelProcessor())
                .Build();
        }

        public virtual string Language { get; set; } = "RU";

        public virtual BackupType BackupType { get; set; } = BackupType.Titanium;

        public virtual string PopupMessage { get; set; }

        public virtual string Theme { get; set; } = "Light";

        public virtual bool AlternativeSigning { get; set; }

        public virtual bool Notifications { get; set; } = true;
    }
}
