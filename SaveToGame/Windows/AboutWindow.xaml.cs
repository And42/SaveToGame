using System.Windows;
using System.Windows.Controls;
using Interfaces.ViewModels;
using JetBrains.Annotations;
using SaveToGameWpf.Logic.Utils;

namespace SaveToGameWpf.Windows
{
    public partial class AboutWindow
    {
        public AboutWindow(
            [NotNull] IAboutWindowViewModel viewModel
        )
        {
            InitializeComponent();

            DataContext = viewModel;
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
