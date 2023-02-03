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

        public static MainWindow? MW { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            Utils.Log("Application start");
            Mapsets.Add(new(Localization.Strings.MapTypeBase, "base-m", 19));
            Mapsets.Add(new(Localization.Strings.MapTypeTransport, "base-m-traf-down", 19));
            Mapsets.Add(new(Localization.Strings.MapTypeOrto, "ophoto-m", 20));
            Mapsets.Add(new(Localization.Strings.MapTypeOrto2018, "ophoto1618-m", 20));
            Mapsets.Add(new(Localization.Strings.MapTypeOrto2015, "ophoto1415-m", 20));
            Mapsets.Add(new(Localization.Strings.MapTypeOrto2012, "ophoto1012-m", 19));
            Mapsets.Add(new(Localization.Strings.MapTypeOrto2006, "ophoto0406-m", 19));
            Mapsets.Add(new(Localization.Strings.MapTypeOrto2003, "ophoto0203-m", 18));
            Mapsets.Add(new(Localization.Strings.MapTypeTourist, "turist-m", 19));
            Mapsets.Add(new(Localization.Strings.MapTypeGeo, "zemepis-m", 18));
            Mapsets.Add(new(Localization.Strings.MapTypeWinter, "winter-m-down", 19));
            Mapsets.Add(new(Localization.Strings.MapTypeHist, "army2-m", 15));
            Mapsets.Add(new(Localization.Strings.MapTypeRelief, "relief-m", 15));

            DownloadClient.DefaultRequestHeaders.Add("Referer", "https://mapy.cz/");

            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.Save();
            }
        }
    }
}
