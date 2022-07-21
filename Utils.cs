using MapyCZforTS_CS.Properties;
using Microsoft.Win32;
using System;
using System.Linq;
using System.IO;
using System.Windows;
using Titanium.Web.Proxy;
using System.Runtime.InteropServices;

namespace MapyCZforTS_CS
{
    internal class Utils
    {
        /// <summary>
        /// Logs exception.
        /// </summary>
        /// <param name="e">Exception to log.</param>
        /// <param name="level">Message level (0 - DEFAULT, 1 - VERBOSE)</param>
        public static void Log(Exception e, byte level = 0)
        {
            Log(e.Message, level);
        }

        /// <summary>
        /// Logs message.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="level">Message level (0 - DEFAULT, 1 - VERBOSE)</param>
        public static void Log(string message, byte level = 0)
        {
            //TODO: implement logging
        }

        private const string CACHED_TILE_PATTERN = "*staticmap*";

        public static void DeleteCachedTiles(DirectoryInfo cacheDir)
        {
            foreach (var cachedTile in cacheDir.GetFiles(CACHED_TILE_PATTERN, SearchOption.AllDirectories))
            {
                try
                {
                    cachedTile.Delete();
                }
                catch { }
            }
        }

        public static void CleanIECache()
        {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                try
                {
                    var newCache = new DirectoryInfo(Path.Join(appdata, "Microsoft", "Windows", "INetCache", "IE"));
                    if (newCache.Exists)
                    {
                        DeleteCachedTiles(newCache);
                    }
                } catch { }

                try
                {
                    var oldCache = new DirectoryInfo(Path.Join(appdata, "Microsoft", "Windows", "Temporary Internet Files", "Content.IE5"));
                    if (oldCache.Exists)
                    {
                        DeleteCachedTiles(oldCache);
                    }
                }
                catch { }
        }

        private static string? OldProxyHost;
        private static string? OldProxyBypass;
        private static string? OldProxyAutoconfig;
        private static int OldProxyEnabled;

        public static void DisableProxy()
        {
            try
            {
                var hreg = Registry.CurrentUser.CreateSubKey(Path.Join("Software", "Microsoft", "Windows", "CurrentVersion", "Internet Settings"), true);
                if (hreg != null)
                {
                    if (OldProxyHost != null)
                    {
                        hreg.SetValue("ProxyServer", OldProxyHost);
                    }

                    if (OldProxyBypass != null)
                    {
                        hreg.SetValue("ProxyOverride", OldProxyBypass);
                    }

                    if (OldProxyAutoconfig != null)
                    {
                        hreg.SetValue("AutoConfigURL", OldProxyAutoconfig);
                    }

                    hreg.SetValue("ProxyEnable", OldProxyEnabled);
                }
            } catch
            {
                string host = string.Empty, port = string.Empty;
                if (OldProxyHost != null)
                {
                    Uri uri = new Uri(OldProxyHost);
                    host = uri.Host;
                    port = uri.Port.ToString();
                }

                string bypasses = string.Empty;
                string excludeLocalhost = Localization.Strings.Unchecked;
                if (OldProxyBypass != null)
                {
                    bypasses = string.Join(';', OldProxyBypass.Split(';').Where(
                        x => {
                            if (x.ToLower() != "<local>")
                                return true;

                            excludeLocalhost = Localization.Strings.Checked;
                            return false;
                        }
                    ));
                }
                MessageBox.Show(string.Format(Localization.Strings.ContentRestoreProxy, host, port, bypasses, excludeLocalhost), Localization.Strings.TitleRestoreProxy, MessageBoxButton.OK, MessageBoxImage.Warning);
            } finally
            {
                OldProxyHost = null;
                OldProxyBypass = null;
                OldProxyAutoconfig = null;
                OldProxyEnabled = 0;
                RefreshProxy();
            }
        }

        public static Proxy EnableProxy()
        {
            try
            {
                var hreg = Registry.CurrentUser.CreateSubKey(Path.Join("Software", "Microsoft", "Windows", "CurrentVersion", "Internet Settings"), true);
                if (hreg != null)
                {
                    OldProxyHost = (string?)hreg.GetValue("ProxyServer");
                    if (OldProxyHost != null && !OldProxyHost.StartsWith("http://"))
                        OldProxyHost = "http://" + OldProxyHost;

                    OldProxyEnabled = (int?)hreg.GetValue("ProxyEnable") ?? 0;
                    OldProxyBypass = (string?)hreg.GetValue("ProxyOverride");
                    OldProxyAutoconfig = (string?)hreg.GetValue("AutoConfigURL");

                    try
                    {
                        SetProxyPort(hreg, Settings.Default.Port);
                        hreg.SetValue("ProxyEnable", 1);
                        hreg.DeleteValue("ProxyOverride", false);
                        hreg.DeleteValue("AutoConfigURL", false);
                        ProxyServer p = new();
                    } catch
                    {
                        MessageBox.Show(string.Format(Localization.Strings.ContentSetProxy, Settings.Default.Port), Localization.Strings.TitleSetProxy, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    if (OldProxyHost != null && OldProxyEnabled == 1)
                    {
                        Uri proxyUri = new(OldProxyHost);
                        if (proxyUri.Host != null) // && !proxyUri.IsDefaultPort)
                        {
                            bool bypassLocalhost = false;
                            if (OldProxyBypass != null)
                            {
                                string[] frags = OldProxyBypass.Split(';');
                                foreach (string frag in frags)
                                {
                                    if (frag.ToLower() == "<local>")
                                    {
                                        bypassLocalhost = true;
                                        break;
                                    }
                                }
                            }

                            RefreshProxy();
                            return new(proxyUri.Host, proxyUri.Port, bypassLocalhost);
                        }
                    }
                }
            } catch
            {
            }

            RefreshProxy();
            return new();
        }

        private static void RefreshProxy()
        {
            InternetSetOption(IntPtr.Zero, 39, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, 37, IntPtr.Zero, 0);
        }

        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        public static void SetProxyPort(RegistryKey? hreg, int port) => hreg?.SetValue("ProxyServer", $"http://localhost:{port}");
    }
}
