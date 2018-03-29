using System.ComponentModel;

namespace SaveToGameWpf.Logic.Interfaces
{
    public interface IRaisePropertyChanged : INotifyPropertyChanged
    {
        void RaisePropertyChanged(string propertyName);
    }
}