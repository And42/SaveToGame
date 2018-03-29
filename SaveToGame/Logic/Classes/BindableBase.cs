using System.ComponentModel;
using System.Runtime.CompilerServices;
using SaveToGameWpf.Annotations;

namespace SaveToGameWpf.Logic.Classes
{
    public class BindableBase : INotifyPropertyChanged
    {
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            RaisePropertyChanged(propertyName);

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
