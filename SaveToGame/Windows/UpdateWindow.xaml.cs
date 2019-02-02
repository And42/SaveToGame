using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using JetBrains.Annotations;
using SaveToGameWpf.Logic.Utils;

// ReSharper disable PossibleNullReferenceException

namespace SaveToGameWpf.Windows
{
    public partial class UpdateWindow
    {
        [NotNull] private readonly Provider<DownloadWindow> _downloadWindowProvider;

        public UpdateWindow(
            [NotNull] Provider<DownloadWindow> downloadWindowProvider,
            string nowVersion,
            string changes
        )
        {
            _downloadWindowProvider = downloadWindowProvider;

            InitializeComponent();

            var xdoc = new XmlDocument();
            xdoc.LoadXml(changes);

            var versions = xdoc.GetElementsByTagName("version").Cast<XmlNode>().Where(node =>
                Utils.CompareVersions(node.Attributes["version"].InnerText, nowVersion) >= 0);

            var sb = new StringBuilder();
            foreach (var version in versions)
            {
                sb.Append(version.Attributes["version"].InnerText + ":");

                foreach (XmlNode item in version.ChildNodes)
                    sb.Append(Environment.NewLine + " - " + item.InnerText);

                sb.Append(Environment.NewLine);
                sb.Append(Environment.NewLine);
            }

            ChangesBox.Text = sb.ToString();
        }

        private void YesClick(object sender, RoutedEventArgs e)
        {
             _downloadWindowProvider.Get().ShowDialog();
        }

        private void NoClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
