using System.Windows.Media.Imaging;
using SaveToGameWpf.Logic.Utils;

// ReSharper disable InconsistentNaming

namespace SaveToGameWpf.Logic.Classes
{
    public class AppIconsStorage : BindableBase
    {
        public BitmapSource Icon_xxhdpi
        {
            get => _icon_xxhdpi;
            private set => SetProperty(ref _icon_xxhdpi, value);
        }
        private BitmapSource _icon_xxhdpi;

        public byte[] Icon_xxhdpi_array
        {
            get => _icon_xxhdpi_array;
            set
            {
                if (SetProperty(ref _icon_xxhdpi_array, value))
                    Icon_xxhdpi = value.ToBitmap().ToBitmapSource();
            }
        }
        private byte[] _icon_xxhdpi_array;

        public BitmapSource Icon_xhdpi
        {
            get => _icon_xhdpi;
            private set => SetProperty(ref _icon_xhdpi, value);
        }
        private BitmapSource _icon_xhdpi;

        public byte[] Icon_xhdpi_array
        {
            get => _icon_xhdpi_array;
            set
            {
                if (SetProperty(ref _icon_xhdpi_array, value))
                    Icon_xhdpi = value.ToBitmap().ToBitmapSource();
            }
        }
        private byte[] _icon_xhdpi_array;

        public BitmapSource Icon_hdpi
        {
            get => _icon_hdpi;
            private set => SetProperty(ref _icon_hdpi, value);
        }
        private BitmapSource _icon_hdpi;

        public byte[] Icon_hdpi_array
        {
            get => _icon_hdpi_array;
            set
            {
                if (SetProperty(ref _icon_hdpi_array, value))
                    Icon_hdpi = value.ToBitmap().ToBitmapSource();
            }
        }
        private byte[] _icon_hdpi_array;

        public BitmapSource Icon_mdpi
        {
            get => _icon_mdpi;
            private set => SetProperty(ref _icon_mdpi, value);
        }
        private BitmapSource _icon_mdpi;

        public byte[] Icon_mdpi_array
        {
            get => _icon_mdpi_array;
            set
            {
                if (SetProperty(ref _icon_mdpi_array, value))
                    Icon_mdpi = value.ToBitmap().ToBitmapSource();
            }
        }
        private byte[] _icon_mdpi_array;
    }
}
