using System;
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
        /// Default constructor
        /// </summary>
        public Proxy()
        {
            ProxyServer = new ProxyServer();

            ProxyServer.BeforeRequest += OnRequest; //register callback

            var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, App.Port, false);
            ProxyServer.AddEndPoint(explicitEndPoint);
        }

        /// <summary>
        /// Starts proxy server.
        /// </summary>
        public void Start() => ProxyServer.Start();

        /// <summary>
        /// Stops proxy server.
        /// </summary>
        public void Stop() => ProxyServer.Stop();

        /// <summary>
        /// ProxyServer instance.
        /// </summary>
        private readonly ProxyServer ProxyServer;

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
            e.Respond(response);
        }

        /// <summary>
        /// Checks and parses query string into individual parameters.
        /// </summary>
        /// <param name="e">Proxy event arguments</param>
        /// <param name="query">Input query string</param>
        private static void ParseQuery(SessionEventArgs e, string query)
        {
            var queryAray = HttpUtility.ParseQueryString(query);
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
                int resX = 1024, resY = 1024;

                string[] resolution = queryAray["size"]!.Split("x");
                int.TryParse(resolution[0], out resX);
                int.TryParse(resolution[1], out resY);


                //try parse rescale value, no rescale if failed
                int scale = 1;
                int.TryParse(queryAray["scale"], out scale);


                //try parse zoom level, 19 default
                byte zoom = 19;
                byte.TryParse(queryAray["zoom"], out zoom);

                MakeResponse(e, wgsX, wgsY, resX, resY, scale, zoom);
            }
            catch (Exception ex)
            {
                Utils.Log(ex);
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
                    ParseQuery(e, requestUri.Query);
                }
                else if (method == "POST")
                {
                    if (e.HttpClient.Request.ContentType?.Contains("x-www-form", StringComparison.OrdinalIgnoreCase) == true) //if method is POST, parse received body content
                    {
                        ParseQuery(e, await e.GetRequestBodyAsString());
                    }
                    else if (e.HttpClient.Request.ContentType == null) //if method is POST, but does not contain body encoded data, fail request
                    {
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
