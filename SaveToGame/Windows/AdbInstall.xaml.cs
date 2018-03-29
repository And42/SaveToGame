using System.Collections.ObjectModel;

namespace SaveToGameWpf.Windows
{
    /// <summary>
    /// Логика взаимодействия для AdbInstall.xaml
    /// </summary>
    public partial class AdbInstall
    {
        public ObservableCollection<Device> Devices { get; } = new ObservableCollection<Device>(); 

        public class Device
        {
            public string Title { get; set; }
        }

        public AdbInstall()
        {
            InitializeComponent();
        }

        public void UpdateDevices()
        {
            
        }
    }
}
