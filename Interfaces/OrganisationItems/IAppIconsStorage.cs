using System.Windows.Media.Imaging;
using MVVM_Tools.Code.Providers;

namespace Interfaces.OrganisationItems
{
    public interface IAppIconsStorage
    {
        IProperty<BitmapSource> Icon_xxhdpi { get; }
        IProperty<BitmapSource> Icon_xhdpi { get; }
        IProperty<BitmapSource> Icon_hdpi { get; }
        IProperty<BitmapSource> Icon_mdpi { get; }

        byte[] GetXxhdpiBytes();
        byte[] GetXhdpiBytes();
        byte[] GetHdpiBytes();
        byte[] GetMdpiBytes();
    }
}
