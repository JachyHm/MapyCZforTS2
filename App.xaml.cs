using MapyCZforTS_CS.Properties;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;

namespace MapyCZforTS_CS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// List of all mapsets
        /// </summary>
        public static List<Mapset> Mapsets { get; set; } = new();

        /// <summary>
        /// HttpClient for downloading source tiles.
        /// </summary>
        public static HttpClient DownloadClient { get; set; } = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            Mapsets.Add(new("Základní", "base-m", 19));
            Mapsets.Add(new("Dopravní", "base-m-traf-down", 19));
            Mapsets.Add(new("Letecká", "bing", 20));
            Mapsets.Add(new("Letecká 2018", "ophoto1618-m", 20));
            Mapsets.Add(new("Letecká 2015", "ophoto1415-m", 20));
            Mapsets.Add(new("Letecká 2012", "ophoto1012-m", 19));
            Mapsets.Add(new("Letecká 2006", "ophoto0406-m", 19));
            Mapsets.Add(new("Letecká 2003", "ophoto0203-m", 18));
            Mapsets.Add(new("Turistická", "turist-m", 19));
            Mapsets.Add(new("Zeměpisná", "zemepis-m", 18));
            Mapsets.Add(new("Zimní", "winter-m-down", 19));
            Mapsets.Add(new("Historická 1836-1852", "army2-m", 15));
            Mapsets.Add(new("Reliéfní", "relief-m", 15));

            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.Save();
            }
        }
    }
}
