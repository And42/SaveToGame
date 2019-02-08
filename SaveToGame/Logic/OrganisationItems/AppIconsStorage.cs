using System.Windows.Media.Imaging;
using Interfaces.OrganisationItems;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic.Utils;

// ReSharper disable InconsistentNaming

namespace SaveToGameWpf.Logic.OrganisationItems
{
    public class AppIconsStorage : IAppIconsStorage
    {
        public IProperty<BitmapSource> Icon_xxhdpi { get; } = new FieldProperty<BitmapSource>();
        public IProperty<BitmapSource> Icon_xhdpi { get; } = new FieldProperty<BitmapSource>();
        public IProperty<BitmapSource> Icon_hdpi { get; } = new FieldProperty<BitmapSource>();
        public IProperty<BitmapSource> Icon_mdpi { get; } = new FieldProperty<BitmapSource>();

        public byte[] GetXxhdpiBytes() => GetBytes(Icon_xxhdpi);
        public byte[] GetXhdpiBytes() => GetBytes(Icon_xhdpi);
        public byte[] GetHdpiBytes() => GetBytes(Icon_hdpi);
        public byte[] GetMdpiBytes() => GetBytes(Icon_mdpi);

        private static byte[] GetBytes(IReadonlyProperty<BitmapSource> property) => property.Value.ToBitmap().ToByteArray();
    }
}
