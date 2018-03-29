using System;
using SaveToGameWpf.Logic.Utils;

namespace SaveToGameWpf.Windows
{
    /// <summary>
    /// Логика взаимодействия для AboutFormCS.xaml
    /// </summary>
    public partial class AboutWindow
    {   
        public string Version => ApplicationUtils.GetVersion();

        public AboutWindow()
        {
            InitializeComponent();
        }
        
        private void DeveloperBtn_Clicked(object sender, EventArgs e)
        {
            Utils.OpenLinkInBrowser("http://www.4pda.ru/forum/index.php?showuser=2114045");
        }

        private void FourPdaBtn_Clicked(object sender, EventArgs e)
        {
            Utils.OpenLinkInBrowser("http://4pda.ru/forum/index.php?act=rep&type=win_add&mid=2114045&p=23243303");
        }
    }
}
