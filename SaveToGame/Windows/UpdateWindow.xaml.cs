using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using SaveToGameWpf.Logic.Utils;

// ReSharper disable PossibleNullReferenceException

namespace SaveToGameWpf.Windows
{
    public partial class UpdateWindow
    {
        public UpdateWindow(string nowVersion, string changes)
        {
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
            //if (ApplicationUtils.GetIsAdmin())
            //{
                Close();
                new DownloadWindow().ShowDialog();
            /*}
            else
            {
                if (
                    MessBox.ShowDial(MainResources.UpdateAdminRequired, null, MessBox.MessageButtons.Yes,
                        MessBox.MessageButtons.No) == MessBox.MessageButtons.Yes)
                {
                    Utils.RunAsAdmin(ApplicationUtils.GetPathToExe(), "");
                    Environment.Exit(0);
                }
            }*/
        }

        private void NoClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
