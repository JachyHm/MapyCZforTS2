using MapyCZforTS_CS.Properties;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace MapyCZforTS_CS
{
    /// <summary>
    /// ProxyServer wrapper class
    /// </summary>
    public class Proxy
    {
        /// <summary>
        /// ProxyServer instance.
        /// </summary>
        private readonly ProxyServer ProxyServer;

        public bool ProxyRunning => ProxyServer.ProxyRunning;
        private readonly int Port;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Proxy(string? externalProxyHostname = null, int externalProxyPort = 0, bool bypassLocalhost = false)
        {
            Utils.Log($"PROXY -> ctor:{Environment.NewLine}\texternalProxyHostname: {externalProxyHostname}{Environment.NewLine}\texternalProxyPort: {externalProxyPort}{Environment.NewLine}\tbypassLocalhost: {bypassLocalhost}", Utils.LOG_LEVEL.VERBOSE);
            ProxyServer = new ProxyServer();

            ProxyServer.BeforeRequest += OnRequest; //register callback

            Port = Settings.Default.Port;
            ExplicitProxyEndPoint explicitEndPoint = new(IPAddress.Any, Port, false);
            ProxyServer.AddEndPoint(explicitEndPoint);

            if (externalProxyHostname != null)
            {
                IPAddress.TryParse(externalProxyHostname, out IPAddress? externalProxyIP);
                if (externalProxyHostname.Trim().ToLower() == "localhost" || (externalProxyIP != null && IPAddress.IsLoopback(externalProxyIP)) && externalProxyPort == Settings.Default.Port)
                    return;

                ProxyServer.UpStreamHttpProxy = new ExternalProxy() { HostName = externalProxyHostname, Port = externalProxyPort, BypassLocalhost = bypassLocalhost };
                ProxyServer.UpStreamHttpsProxy = new ExternalProxy() { HostName = externalProxyHostname, Port = externalProxyPort, BypassLocalhost = bypassLocalhost };
            }
        }

        /// <summary>
        /// Starts proxy server.
        /// </summary>
        public void Start()
        {
            Utils.Log($"PROXY -> Starting on port {Port}");
            try
            {
                ProxyServer.Start();
            }
            catch (Exception e) { Utils.Log($"PROXY -> Failed to start proxy on port {Port}:{Environment.NewLine}{e}"); }
        }

        /// <summary>
        /// Stops proxy server.
        /// </summary>
        public void Stop()
        {
            Utils.Log($"PROXY -> Stopping proxy");
            try
            {
                ProxyServer.Stop();
            }
            catch (Exception e) { Utils.Log($"PROXY -> Failed to stop proxy on port {Port}:{Environment.NewLine}{e}"); }
        }

        /// <summary>
        /// Changes port Proxy is listening on.
        /// </summary>
        /// <param name="newPort">New port to listen on.</param>
        public void ChangePort(int newPort)
        {
            Utils.Log($"PROXY -> Changing proxy port at runtime");
            try
            {
                foreach (var oldEndpoint in ProxyServer.ProxyEndPoints)
                {
                    ProxyServer.RemoveEndPoint(oldEndpoint);
                }

                ExplicitProxyEndPoint newEndpoint = new(IPAddress.Any, newPort, false);
                ProxyServer.AddEndPoint(newEndpoint);
            }
            catch (Exception e) { Utils.Log($"PROXY -> Failed to change proxy port to {newPort}:{Environment.NewLine}{e}"); }
        }

        /// <summary>
        /// Generates ProxyServer response with output tile. 
        /// </summary>
        /// <param name="e">Proxy event arguments</param>
        /// <param name="wgsX">Decoded WGS84 x coord</param>
        /// <param name="wgsY">Decoded WGS84 y coord</param>
        /// <param name="resX">Decoded horizontal resolution</param>
        /// <param name="resY">Decoded vertical resolution</param>
        /// <param name="scale">Decoded rescale value</param>
        /// <param name="zoom">Decoded zoom level</param>
        private static void MakeResponse(SessionEventArgs e, double wgsX, double wgsY, int resX, int resY, int scale, byte zoom)
        {
            //create output tile
            MapTile img = new(wgsX, wgsY, resX, resY, scale, zoom);
            img.Get();

            using MemoryStream ms = new();
            img.OutImage?.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            //generate the response (headers from original gapi response for compatibility)
            //INFO: most of them isn't probably needed?
            Response response = new(ms.ToArray())
            {
                StatusCode = 200,
                HttpVersion = HttpVersion.Version11
            };
            response.Headers.AddHeader(new HttpHeader("Content-Type", "image/jpeg"));
            response.Headers.AddHeader(new HttpHeader("Cache-Control", "public, max-age=86400"));
            response.Headers.AddHeader(new HttpHeader("Vary", "Accept-Language"));
            response.Headers.AddHeader(new HttpHeader("Access-Control-Allow-Origin", "*"));
            response.Headers.AddHeader(new HttpHeader("X-XSS-Protection", "1; mode=block"));
            response.Headers.AddHeader(new HttpHeader("X-Frame-Options", "SAMEORIGIN"));
            Utils.Log($"PROXY -> Response to {e.ClientConnectionId} succesfully sent", Utils.LOG_LEVEL.VERBOSE);
            e.Respond(response);
        }

        /// <summary>
        /// Checks and parses query string into individual parameters.
        /// </summary>
        /// <param name="e">Proxy event arguments</param>
        /// <param name="query">Input query string</param>
        private static void ParseQuery(SessionEventArgs e, string query)
        {
            Utils.Log($"PROXY -> Parsing query {query}", Utils.LOG_LEVEL.VERBOSE);
            NameValueCollection queryAray = HttpUtility.ParseQueryString(query);
            try
            {
                if (queryAray["center"] == null) //check for tile center coordinates - can't continue unless we know them
                    throw new ArgumentException("Obligatory 'center' parameter is missing!");

                /*if (query["size"] == null)
                    throw new ArgumentException("Obligatory 'size' parameter is missing!");*/

                //try parse center coordinates, throw exception if failed
                string[] center = queryAray["center"]!.Split(",");
                if (!double.TryParse(center[0], out double wgsY))
                    throw new FormatException("Invalid format of Y coordinate!");

                if (!double.TryParse(center[1], out double wgsX))
                    throw new FormatException("Invalid format of X coordinate!");


                //try parse resolution, fallback to 1024x1024 px

                string[] resolution = queryAray["size"]!.Split("x");
                int.TryParse(resolution[0], out int resX);
                int.TryParse(resolution[1], out int resY);


                //try parse rescale value, no rescale if failed
                int.TryParse(queryAray["scale"], out int scale);


                //try parse zoom level, 19 default
                byte.TryParse(queryAray["zoom"], out byte zoom);

                Utils.Log($"PROXY -> Query OK, generating response", Utils.LOG_LEVEL.VERBOSE);
                MakeResponse(e, wgsX, wgsY, resX, resY, scale, zoom);
            }
            catch (Exception ex)
            {
                Utils.Log($"PROXY -> Failed to parse query {query}:{Environment.NewLine}{ex}");
                throw;
            }
        }

        /// <summary>
        /// Callback function called before further request processing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Proxy event arguments</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private async Task OnRequest(object sender, SessionEventArgs e)
        {
            string method = e.HttpClient.Request.Method.ToUpper(); //get request method
            Uri requestUri = e.HttpClient.Request.RequestUri; //get request uri
            if (requestUri.AbsoluteUri.Contains("maps.googleapis.com")) //if uri is map tile
            {
                if (method == "GET") //if method is GET, parse directly the query string
                {
                    Utils.Log($"PROXY -> Received GET request {requestUri}", Utils.LOG_LEVEL.VERBOSE);
                    ParseQuery(e, requestUri.Query);
                }
                else if (method == "POST")
                {
                    if (e.HttpClient.Request.ContentType?.Contains("x-www-form", StringComparison.OrdinalIgnoreCase) == true) //if method is POST, parse received body content
                    {
                        Utils.Log($"PROXY -> Received POST www-form request {requestUri}", Utils.LOG_LEVEL.VERBOSE);
                        ParseQuery(e, await e.GetRequestBodyAsString());
                    }
                    else if (e.HttpClient.Request.ContentType == null) //if method is POST, but does not contain body encoded data, fail request
                    {
                        Utils.Log($"PROXY -> Received unknown type POST request {requestUri}", Utils.LOG_LEVEL.VERBOSE);
                        Response response = new()
                        {
                            StatusCode = 400,
                            HttpVersion = HttpVersion.Version11
                        };
                        e.Respond(response);
                    }
                }
            }
        }
    }
}
