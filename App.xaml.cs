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
        /// Port that will be used for ProxyServer.
        /// </summary>
        public static int Port = 5001;

        /// <summary>
        /// Allow to cache source and output tiles.
        /// </summary>
        public static bool Cache = true;

        /// <summary>
        /// Currently selected mapset.
        /// </summary>
        public static string Mapset = "ophoto-m";

        /// <summary>
        /// HttpClient for downloading source tiles.
        /// </summary>
        public static HttpClient DownloadClient = new();
    }
}
