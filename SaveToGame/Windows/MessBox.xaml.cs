using System.Windows;
using System.Windows.Controls;
using SaveToGameWpf.Logic.Utils;
using SaveToGameWpf.Resources.Localizations;

namespace SaveToGameWpf.Windows
{
    public partial class MessBox
    {
        private static string _result;

        public MessBox()
        {
            InitializeComponent();
        }

        public static string ShowDial(string message, string caption = null, params string[] buttons)
        {
            Application.Current.Dispatcher.Invoke(() => new MessBox().ShowD(message, caption, buttons));
            return _result;
        }

        private void ShowD(string message, string caption = null, params string[] buttons)
        {
            Title = caption ?? string.Empty;
            MessLabel.Text = message;

            if (buttons.Length == 0)
            {
                FirstButton.Content = MainResources.OK;
            }
            else
            {
                FirstButton.Content = buttons[0];

                for (var i = 1; i < buttons.Length; i++)
                {
                    var button = new Button {Content = buttons[i]};
                    button.Click += Button_Click;
                    Grid.SetColumn(button, MainGrid.ColumnDefinitions.Count);
                    Grid.SetRow(button, 1);
                    button.Margin = new Thickness(0, 0, 15, 15);
                    button.FontSize = 13;
                    /*var resourceDictionary = new ResourceDictionary
                    {
                        Source = new Uri("/Styles/VS 2012/VS2012WindowStyle.xaml", UriKind.Relative)
                    };
                    button.Template = (ControlTemplate)resourceDictionary["ButtonTemplate"];*/
                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(6, GridUnitType.Star)});
                    MainGrid.Children.Add(button);
                }
            }

            Grid.SetColumnSpan(MessageScroll, buttons.Length == 0 ? 1 : buttons.Length);

            ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _result = sender.As<Button>().Content.As<string>();
            Close();
        } 
    }
}
