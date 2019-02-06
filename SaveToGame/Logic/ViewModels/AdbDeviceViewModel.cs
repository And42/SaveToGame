using MVVM_Tools.Code.Providers;

namespace SaveToGameWpf.Logic.ViewModels
{
    public class AdbDeviceViewModel
    {
        public Property<string> Id { get; } = new Property<string>();
        public Property<string> Title { get; } = new Property<string>();
    }
}
