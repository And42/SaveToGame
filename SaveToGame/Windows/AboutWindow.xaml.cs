using System;
using JetBrains.Annotations;
using SaveToGameWpf.Logic.Utils;

namespace SaveToGameWpf.Windows
{
    public partial class AboutWindow
    {
        [NotNull] private readonly ApplicationUtils _applicationUtils;

        public string Version => _applicationUtils.GetVersion();

        public AboutWindow(
            [NotNull] ApplicationUtils applicationUtils
        )
        {
            _applicationUtils = applicationUtils;
            InitializeComponent();
        }
        
        private void DeveloperBtn_Clicked(object sender, EventArgs e)
        {
            WebUtils.OpenLinkInBrowser("http://www.4pda.ru/forum/index.php?showuser=2114045");
        }

        private void FourPdaBtn_Clicked(object sender, EventArgs e)
        {
            WebUtils.OpenLinkInBrowser("http://4pda.ru/forum/index.php?act=rep&type=win_add&mid=2114045&p=23243303");
        }
    }
}
