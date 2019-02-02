using System.ComponentModel;
using Interfaces.Enums;

namespace Interfaces.OrganisationItems
{
    public interface IAppSettings : INotifyPropertyChanged
    {
        string Language { get; set; }

        BackupType BackupType { get; set; }

        string PopupMessage { get; set; }

        string Theme { get; set; }

        bool AlternativeSigning { get; set; }

        bool Notifications { get; set; }
    }
}