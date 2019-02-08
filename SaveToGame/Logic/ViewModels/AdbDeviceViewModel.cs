using MVVM_Tools.Code.Providers;

namespace SaveToGameWpf.Logic.ViewModels
{
    public class AdbDeviceViewModel
    {
        public IProperty<string> Id { get; } = new FieldProperty<string>();
        public IProperty<string> Title { get; } = new FieldProperty<string>();
    }
}
