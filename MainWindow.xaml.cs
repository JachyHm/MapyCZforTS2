using MapyCZforTS_CS.Properties;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace MapyCZforTS_CS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Proxy? proxy;
        private readonly bool init = false;

        public MainWindow()
        {
            InitializeComponent();

            App.Mapsets.ForEach(x => mapsetInput.Items.Add(x));
            mapsetInput.SelectedIndex = Settings.Default.Mapset;
            portInput.Value = Settings.Default.Port;
            cachingCheckbox.IsChecked = Settings.Default.Cache;
            loggingCheckbox.IsChecked = Settings.Default.AdvancedLogging;

            init = true;
        }

        private void mapsetInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.Mapset = mapsetInput.SelectedIndex;
            Settings.Default.Save();
            Utils.CleanIECache();
        }

        private void toogleProxy_Click(object sender, RoutedEventArgs e)
        {
            if (proxy?.ProxyRunning == true)
            {
                Utils.DisableProxy();
                proxy.Stop();
                proxy = null;
                toogleProxy.Content = "Zapnout proxy";
            }
            else
            {
                proxy = Utils.EnableProxy();
                proxy.Start();
                toogleProxy.Content = "Vypnout proxy";
            }
            Utils.CleanIECache();
        }

        private void loggingCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (init)
            {
                Settings.Default.AdvancedLogging = loggingCheckbox.IsChecked == true;
                Settings.Default.Save();
            }
        }

        private void cachingCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (init)
            {
                Settings.Default.Cache = cachingCheckbox.IsChecked == true;
                Settings.Default.Save();
            }
        }

        private void portInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (init)
            {
                int newPort = (int)portInput.Value;
                Settings.Default.Port = newPort;
                Settings.Default.Save();

                if (proxy != null)
                {
                    proxy.ChangePort(newPort);
                    Utils.SetProxyPort(
                        Registry.CurrentUser.CreateSubKey(Path.Join("Software", "Microsoft", "Windows", "CurrentVersion", "Internet Settings")),
                        newPort
                    );
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (proxy?.ProxyRunning == true)
            {
                Utils.DisableProxy();
                proxy.Stop();
                proxy = null;
            }
            base.OnClosing(e);
        }
    }
}
