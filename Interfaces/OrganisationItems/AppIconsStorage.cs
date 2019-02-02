using System.Windows.Media.Imaging;
using MVVM_Tools.Code.Providers;

namespace Interfaces.OrganisationItems
{
    public interface IAppIconsStorage
    {
        Property<BitmapSource> Icon_xxhdpi { get; }
        Property<BitmapSource> Icon_xhdpi { get; }
        Property<BitmapSource> Icon_hdpi { get; }
        Property<BitmapSource> Icon_mdpi { get; }

        byte[] GetXxhdpiBytes();
        byte[] GetXhdpiBytes();
        byte[] GetHdpiBytes();
        byte[] GetMdpiBytes();
    }
}
