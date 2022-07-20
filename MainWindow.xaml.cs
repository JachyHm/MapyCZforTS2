using MapyCZforTS_CS.Properties;
using System.Windows;
using System.Windows.Controls;

namespace MapyCZforTS_CS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Proxy proxy;
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

            proxy = new();
        }

        private void mapsetInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.Default.Mapset = mapsetInput.SelectedIndex;
            Settings.Default.Save();
        }

        private void toogleProxy_Click(object sender, RoutedEventArgs e)
        {
            if (proxy.ProxyRunning)
            {
                proxy.Stop();
                toogleProxy.Content = "Zapnout proxy";
            }
            else
            {
                proxy.Start();
                toogleProxy.Content = "Vypnout proxy";
            }
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
                Settings.Default.Port = (int)portInput.Value;
                Settings.Default.Save();
            }
        }
    }
}
