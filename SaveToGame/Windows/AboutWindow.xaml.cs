using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JetBrains.Annotations;
using SaveToGameWpf.Logic;
using SaveToGameWpf.Logic.Utils;

namespace SaveToGameWpf.Windows
{
    public partial class AboutWindow
    {
        [NotNull] private readonly ApplicationUtils _applicationUtils;
        [NotNull] private readonly GlobalVariables _globalVariables;

        public string Version => _applicationUtils.GetVersion();

        public AboutWindow(
            [NotNull] ApplicationUtils applicationUtils,
            [NotNull] GlobalVariables globalVariables
        )
        {
            _applicationUtils = applicationUtils;
            _globalVariables = globalVariables;
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

        private void DataFolder_Clicked(object sender, MouseButtonEventArgs e)
        {
            Process.Start(_globalVariables.AppDataPath);
        }

        private void Link_MouseEnter(object sender, RoutedEventArgs e)
        {
            sender.As<TextBlock>().TextDecorations.Add(TextDecorations.Underline);
        }

        private void Link_MouseLeave(object sender, RoutedEventArgs e)
        {
            sender.As<TextBlock>().TextDecorations.Clear();
        }
    }
}
