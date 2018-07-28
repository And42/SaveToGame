using System.Windows.Media.Imaging;
using MVVM_Tools.Code.Providers;
using SaveToGameWpf.Logic.Utils;

// ReSharper disable InconsistentNaming

namespace SaveToGameWpf.Logic.Classes
{
    public class AppIconsStorage
    {
        public Property<BitmapSource> Icon_xxhdpi { get; } = new Property<BitmapSource>();
        public Property<BitmapSource> Icon_xhdpi { get; } = new Property<BitmapSource>();
        public Property<BitmapSource> Icon_hdpi { get; } = new Property<BitmapSource>();
        public Property<BitmapSource> Icon_mdpi { get; } = new Property<BitmapSource>();

        public byte[] GetXxhdpiBytes() => GetBytes(Icon_xxhdpi);
        public byte[] GetXhdpiBytes() => GetBytes(Icon_xhdpi);
        public byte[] GetHdpiBytes() => GetBytes(Icon_hdpi);
        public byte[] GetMdpiBytes() => GetBytes(Icon_mdpi);

        private static byte[] GetBytes(Property<BitmapSource> property) => property.Value.ToBitmap().ToByteArray();
    }
}
