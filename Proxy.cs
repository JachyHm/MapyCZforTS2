using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace MapyCZforTS_CS
{
    public class ProxyServer : IDisposable
    {
        private const string UriPrefix = "http://maps.googleapis.com/";

        private readonly HttpListener _listener;
        private static readonly HttpClient _client = new();

        public ProxyServer(int port)
        {
            _listener = new HttpListener();
            Prefixes.Add($"http://127.0.0.1:{port}/");
            Prefixes.Add($"http://localhost:{port}/");
            foreach (string prefix in Prefixes)
            {
                _listener.Prefixes.Add(prefix);
            }
        }

        public List<string> Prefixes { get; set; } = new();

        public void Start()
        {
            _listener.Start();
            _listener.BeginGetContext(ProcessRequest, null);
        }

        private async void ProcessRequest(IAsyncResult result)
        {
            if (!_listener.IsListening)
                return;

            var ctx = _listener.EndGetContext(result);
            _listener.BeginGetContext(ProcessRequest, null);
            await ProcessRequest(ctx).ConfigureAwait(false);
        }

        protected virtual async Task ProcessRequest(HttpListenerContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.Request.Url?.Host.IndexOf(UriPrefix, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var query = HttpUtility.ParseQueryString(context.Request.Url?.ToString() ?? String.Empty);

                try
                {
                    if (query["center"] == null)
                        throw new ArgumentException("Obligatory 'center' parameter is missing!");

                    if (query["size"] == null)
                        throw new ArgumentException("Obligatory 'size' parameter is missing");

                    string[] center = query["center"]!.Split(",");
                    if (!double.TryParse(center[0], out double wgsY))
                        throw new FormatException("Invalid format of Y coordinate!");

                    if (!double.TryParse(center[1], out double wgsX))
                        throw new FormatException("Invalid format of X coordinate!");


                    string[] resolution = query["size"]!.Split("x");
                    if (!int.TryParse(center[0], out int resX))
                        throw new FormatException("Invalid format of X resolution!");

                    if (!int.TryParse(center[1], out int resY))
                        throw new FormatException("Invalid format of Y resolution!");


                    if (!int.TryParse(query["scale"], out int scale))
                        throw new FormatException("Invalid scale format");

                    if (!byte.TryParse(query["zoom"], out byte zoom))
                        throw new FormatException("Invalid zoom format");

                    Image img = new Image(wgsX, wgsY, resX, resY, scale, zoom);
                    string fpath = img.Get();

                    context.Response.StatusCode = 200;
                    context.Response.Headers.Add("Content-type", "image/jpeg");
                    context.Response.Headers.Add("Cache-Control", "public, max-age=86400");
                    context.Response.Headers.Add("Vary", "Accept-Language");
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");

                    using (var os = context.Response.OutputStream)
                    {
                        using (MemoryStream ms = new())
                        {
                            img.OutImage?.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            await ms.CopyToAsync(os).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e)
                {
                    Utils.Log(e);
                    throw;
                }
            }
            else
            {
                using var msg = new HttpRequestMessage(new HttpMethod(context.Request.HttpMethod), context.Request.UserHostAddress + context.Request.RawUrl);
                using var response = await _client.SendAsync(msg).ConfigureAwait(false);
                using var os = context.Response.OutputStream;
                context.Response.ProtocolVersion = response.Version;
                context.Response.StatusCode = (int)response.StatusCode;
                context.Response.StatusDescription = response.ReasonPhrase ?? String.Empty;

                foreach (var header in response.Headers)
                {
                    context.Response.Headers.Add(header.Key, string.Join(", ", header.Value));
                }

                foreach (var header in response.Content.Headers)
                {
                    if (header.Key == "Content-Length") // this will be set automatically at dispose time
                        continue;

                    context.Response.Headers.Add(header.Key, string.Join(", ", header.Value));
                }

                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await stream.CopyToAsync(context.Response.OutputStream).ConfigureAwait(false);
            }
        }

        public void Stop() => _listener.Stop();
        public void Dispose() => ((IDisposable)_listener)?.Dispose();
    }
}
