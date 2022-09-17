using MapyCZforTS_CS.Localization;
using MapyCZforTS_CS.Properties;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Titanium.Web.Proxy;

namespace MapyCZforTS_CS
{

    internal class Utils
    {
        public enum LOG_LEVEL
        {
            ERROR,
            VERBOSE
        }

        /// <summary>
        /// Logs message.
        /// </summary>
        /// <param name="message">Message to log.</param>
        /// <param name="level">Message level (0 - ERROR, 1 - VERBOSE)</param>
        public static void Log(string message, LOG_LEVEL level = LOG_LEVEL.ERROR)
        {
            if (level > LogLevel)
                return;

            try
            {
                DateTime dateTime = DateTime.Now;
                if (LogFile == null)
                    LogFile = new(Path.Join(Path.GetTempPath(), $"MapyCZforTS_{dateTime:yyyyMMdd_HHmmss}.log"), false);

                LogFile.WriteLine($"{dateTime:O}: {message}");
                LogFile.Flush();
            }
            catch { }
        }

        private static LOG_LEVEL LogLevel { get => (Settings.Default.AdvancedLogging ? LOG_LEVEL.VERBOSE : LOG_LEVEL.ERROR); }

        private static StreamWriter? LogFile { get; set; }

        private const string CACHED_TILE_PATTERN = "*staticmap*";

        public static void DeleteCachedTiles(DirectoryInfo cacheDir)
        {
            Utils.Log($"CACHE -> Deleting cached tiles in {cacheDir}", LOG_LEVEL.VERBOSE);
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
            }
            catch { }

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
            Utils.Log("PROXY -> Restoring default proxy settings", LOG_LEVEL.VERBOSE);
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
            }
            catch (Exception e)
            {
                Utils.Log($"PROXY -> Failed to restore default proxy settings:{Environment.NewLine}{e}");
                string host = string.Empty, port = string.Empty;
                if (OldProxyHost != null)
                {
                    Uri uri = new(OldProxyHost);
                    host = uri.Host;
                    port = uri.Port.ToString();
                }

                string bypasses = string.Empty;
                string excludeLocalhost = Localization.Strings.Unchecked;
                if (OldProxyBypass != null)
                {
                    bypasses = string.Join(';', OldProxyBypass.Split(';').Where(
                        x =>
                        {
                            if (x.ToLower() != "<local>")
                                return true;

                            excludeLocalhost = Localization.Strings.Checked;
                            return false;
                        }
                    ));
                }
                MessageBox.Show(string.Format(Localization.Strings.ContentRestoreProxy, host, port, bypasses, excludeLocalhost), Localization.Strings.TitleRestoreProxy, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
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
            Log("PROXY -> Applying custom proxy settings", LOG_LEVEL.VERBOSE);
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
                    }
                    catch (Exception e)
                    {
                        Log($"PROXY -> Failed to set registry values:{Environment.NewLine}{e}");
                        MessageBox.Show(string.Format(Strings.ContentSetProxy, Settings.Default.Port), Strings.TitleSetProxy, MessageBoxButton.OK, MessageBoxImage.Warning);
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
            }
            catch (Exception e)
            {
                Utils.Log($"PROXY -> Failed to set custom proxy settings:{Environment.NewLine}{e}");
            }

            RefreshProxy();
            return new();
        }

        private static void RefreshProxy()
        {
            Utils.Log("PROXY -> Applying networking changes", LOG_LEVEL.VERBOSE);
            try
            {
                InternetSetOption(IntPtr.Zero, 39, IntPtr.Zero, 0);
                InternetSetOption(IntPtr.Zero, 37, IntPtr.Zero, 0);
            }
            catch (Exception e) { Utils.Log($"PROXY -> Failed to apply networking changes:{Environment.NewLine}{e}"); }
        }

        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        public static void SetProxyPort(RegistryKey? hreg, int port) => hreg?.SetValue("ProxyServer", $"http://localhost:{port}");
    }
}
