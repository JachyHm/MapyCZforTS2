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
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    Utils.Log($"Unhandled exception occured:{Environment.NewLine}{ex.StackTrace}", Utils.LOG_LEVEL.ERROR);

                _onClosing();
            };

            Utils.Log("Initializing UI", Utils.LOG_LEVEL.VERBOSE);
            InitializeComponent();

            App.MW = this;

            App.Mapsets.ForEach(x => mapsetInput.Items.Add(x));
            mapsetInput.SelectedIndex = Settings.Default.Mapset;
            portInput.Value = Settings.Default.Port;
            cachingCheckbox.IsChecked = Settings.Default.Cache;
            loggingCheckbox.IsChecked = Settings.Default.AdvancedLogging;
            Utils.Log($"Loading settings:{Environment.NewLine}\tMapset: {App.Mapsets[Settings.Default.Mapset]}{Environment.NewLine}\tPort: {Settings.Default.Port}{Environment.NewLine}\tUse cache: {Settings.Default.Cache}{Environment.NewLine}\tAdvanced log: {Settings.Default.AdvancedLogging}", Utils.LOG_LEVEL.VERBOSE);

            init = true;
        }

        private void mapsetInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            byte maxZoom = App.Mapsets[Settings.Default.Mapset].MaxZoom;
            byte posNum = (byte)(maxZoom > 16 ? 22 - maxZoom : maxZoom - 12);
            string posSide = maxZoom > 16 ? Localization.Strings.FromRight : Localization.Strings.FromLeft;

            MaxZoomText.Text = string.Format(Localization.Strings.MaxZoom, maxZoom, posNum, posSide);

            if (!init)
                return;

            Settings.Default.Mapset = mapsetInput.SelectedIndex;
            Settings.Default.Save();
            Utils.Log($"UI -> Changed mapset to {App.Mapsets[Settings.Default.Mapset]}");

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

        private void openLog_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Utils.LogPath))
                return;

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Utils.LogPath) { UseShellExecute = true });
        }

        private void clearCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Utils.Log($"CACHE -> Deleting local image cache per user request");
                Directory.Delete(MapTile.SourceCache, true);
                Directory.Delete(MapTile.OutCache, true);
                Utils.Log($"CACHE -> Succesfully deleted local image cache per user request");
            }
            catch (Exception ex)
            {
                Utils.Log($"CACHE -> Failed to delete local cache: {ex}", Utils.LOG_LEVEL.ERROR);
            }
        }

        private void loggingCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (init)
            {
                Settings.Default.AdvancedLogging = loggingCheckbox.IsChecked == true;
                Settings.Default.Save();
                Utils.Log(Settings.Default.AdvancedLogging ? "UI -> Enabled advanced logging" : "UI -> Disabled advanced logging");
            }
        }

        private void cachingCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (init)
            {
                Settings.Default.Cache = cachingCheckbox.IsChecked == true;
                Settings.Default.Save();
                Utils.Log(Settings.Default.Cache ? "UI -> Enabled caching" : "UI -> Disabled caching");
            }
        }

        private void portInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (init)
            {
                int newPort = (int)portInput.Value;
                Settings.Default.Port = newPort;
                Settings.Default.Save();
                Utils.Log($"UI -> Port changed to {newPort}");

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

        private void _onClosing()
        {
            if (proxy?.ProxyRunning == true)
            {
                Utils.Log("Closing application");
                Utils.DisableProxy();
                proxy.Stop();
                proxy = null;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _onClosing();
            base.OnClosing(e);
        }
    }
}
