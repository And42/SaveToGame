using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Alphaleonis.Win32.Filesystem;
using Microsoft.Win32;
using SaveToGameWpf.Logic.Utils;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using Res = SaveToGameWpf.Resources.Localizations.MainResources;

namespace SaveToGameWpf.Windows
{
    public partial class ActivateProgramWindow
    {
        private bool _showedPlusses;
        private bool _working;

        public ActivateProgramWindow()
        {
            InitializeComponent();
            PlussesBox.Text = Res.ProVersionPlusses;
        }

        #region Button handlers

        private void GenerateLicenseBtn_Click(object sender, EventArgs e)
        {
            var sd = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".lic",
                Filter = Res.LicenseFiles + @" (*.lic)|*.lic"
            };

            if (sd.ShowDialog() != true)
                return;

            try
            {
                LicensingUtils.GenerateNotActivatedLicense(sd.FileName);
            }
            catch (System.IO.IOException)
            {
                MessBox.ShowDial(Res.GenerateLicenseFileError);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex);
            }

            MessBox.ShowDial(Res.Done + "!");
        }

        private void ChooseLicenseBtn_Click(object sender, RoutedEventArgs e)
        {
            var fd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                DefaultExt = ".licx",
                Filter = Res.LicenseFiles + @" (*.licx)|*.licx",
                Multiselect = false
            };

            if (fd.ShowDialog() != true)
                return;

            byte[] bytes = File.ReadAllBytes(fd.FileName);
            try
            {
                if (LicensingUtils.IsLicenseValid(bytes))
                {
                    Properties.Settings.Default.License = new ArrayList(bytes);
                    Properties.Settings.Default.Save();
                    Utils.ProVersionEnable(true);
                    MessBox.ShowDial(Res.ThanksPurchase);
                    Close();
                }
                else
                {
                    MessBox.ShowDial(Res.LicenseNotValid);
                }
            }
            catch (Exception)
            {
                MessBox.ShowDial(Res.LicenseFileError);
            }
        }

        private void BuyProBtn_Click(object sender, EventArgs e)
        {
            BuyProPopup.IsOpen = !BuyProPopup.IsOpen;
        }

        private void BuyWithWebMoneyWmrBtn_Click(object sender, EventArgs e)
        {
            try
            {
                ClosePopup();
                Clipboard.SetText("R897735207346");
                MessBox.ShowDial(Res.WebMoneyWmrCopied);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex);
            }
        }

        private void BuyWithWebMoneyWmzBtn_Click(object sender, EventArgs e)
        {
            try
            {
                ClosePopup();
                Clipboard.SetText("Z195172706550");
                MessBox.ShowDial(Res.WebMoneyWmzCopied);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex);
            }
        }

        private void ShowPlusesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_working)
                return;

            var timeSpan = TimeSpan.FromSeconds(0.5);

            var anim = new DoubleAnimation(Height, Height + (_showedPlusses ? -1 : 1) * 163, new Duration(timeSpan))
            {
                EasingFunction = new PowerEase { Power = 7 }
            };

            _working = true;

            BeginAnimation(HeightProperty, anim);

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(timeSpan);
                _working = false;
            });

            _showedPlusses = !_showedPlusses;
        }

        #endregion

        private void ClosePopup()
        {
            BuyProPopup.IsOpen = false;
        }

        private static void ShowErrorDialog(Exception ex)
        {
            MessageBox.Show($"{Res.UnknownErrorOccured}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}");
        }
    }
}
