using System.Windows.Media.Imaging;
using MVVM_Tools.Code.Classes;
using MVVM_Tools.Code.Providers;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace SaveToGameWpf.Logic.Classes
{
    public class AppIconsStorage : BindableBase
    {
        public Property<BitmapSource> Icon_xxhdpi { get; private set; }
        public Property<BitmapSource> Icon_xhdpi { get; private set; }
        public Property<BitmapSource> Icon_hdpi { get; private set; }
        public Property<BitmapSource> Icon_mdpi { get; private set; }

        public AppIconsStorage()
        {
            BindProperty(() => Icon_xxhdpi);
            BindProperty(() => Icon_xhdpi);
            BindProperty(() => Icon_hdpi);
            BindProperty(() => Icon_mdpi);
        }
    }
}
