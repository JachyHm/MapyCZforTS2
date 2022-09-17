using MapyCZforTS_CS.Properties;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MapyCZforTS_CS
{
    /// <summary>
    /// Helper class for holding info necessary to generate output tile
    /// </summary>
    public class SourceTiles
    {
        /// <summary>
        /// Helper class for holding info about surrounding source tiles
        /// </summary>
        public class nTiles
        {
            /// <summary>
            /// Tile where target pixel is located
            /// </summary>
            public int CenterTile { get; }

            /// <summary>
            /// Number of preceding tiles needed to generate output tile
            /// </summary>
            public int nTilesBefore { get; }

            /// <summary>
            /// Number of following tiles needed to generate output tile
            /// </summary>
            public int nTilesAfter { get; }

            /// <summary>
            /// Total count of source tiles needed to generate output tile
            /// </summary>
            public int Count => nTilesBefore + 1 + nTilesAfter;

            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="centerTile">Tile where target pixel is located</param>
            /// <param name="nTilesBefore">Number of preceding tiles needed to generate output tile</param>
            /// <param name="nTilesAfter">Total count of source tiles needed to generate output tile</param>
            public nTiles(int centerTile, int nTilesBefore, int nTilesAfter)
            {
                CenterTile = centerTile;
                this.nTilesBefore = nTilesBefore;
                this.nTilesAfter = nTilesAfter;
            }
        }

        /// <summary>
        /// Horizontal source tiles
        /// </summary>
        public nTiles xTiles { get; }

        /// <summary>
        /// Vertical source tiles
        /// </summary>
        public nTiles yTiles { get; }

        /// <summary>
        /// Source tiles data
        /// </summary>
        public byte[,][] Data { get; }


        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="xTiles">Horizontal source tiles</param>
        /// <param name="yTiles">Vertical source tiles</param>
        public SourceTiles(nTiles xTiles, nTiles yTiles)
        {
            this.xTiles = xTiles;
            this.yTiles = yTiles;
            Data = new byte[xTiles.Count, yTiles.Count][];
        }
    }


    /// <summary>
    /// Basic class for generating map tiles
    /// </summary>
    internal class MapTile
    {
        /// <summary>
        /// Gets Mapy.cz map pixel from WGS coordinates and zoom.
        /// </summary>
        /// <param name="x">WGS84 X coordinate</param>
        /// <param name="y">WGS84 Y coordinate</param>
        /// <param name="zoom">Mapy.cz map zoom</param>
        /// <returns>Pair of X and Y coords of pixel.</returns>
        public static (int, int) GetPixelFromWGS(double x, double y, byte zoom)
        {
            //This code is a rewriten original JS code from Mapy.cz front-end, I do not have slightest idea what it does
            uint world_size = (uint)Math.Pow(2, zoom + 8);

            double f = Math.Min(Math.Max(Math.Sin(y * Math.PI / 180), -0.9999), 0.9999);

            double retX = (x + 180) / 360 * world_size;
            double retY = (1 - (0.5 * Math.Log((1 + f) / (1 - f)) / Math.PI)) / 2 * world_size;

            return (Convert.ToInt32(retX), Convert.ToInt32(retY));
        }

        /// <summary>
        /// Source map tiles cache
        /// </summary>
        public static string SourceCache => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapyCZforTS", "source_cache");

        /// <summary>
        /// Output map tiles cache
        /// </summary>
        public static string OutCache => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapyCZforTS", "output_cache");

        /// <summary>
        /// Source tiles data
        /// </summary>
        private SourceTiles SourceTiles { get; }

        /// <summary>
        /// Desired zoom level
        /// </summary>
        public byte Zoom { get; }

        /// <summary>
        /// Desired mapset
        /// </summary>
        public Mapset Mapset { get; }

        /// <summary>
        /// Total number of completely loaded tiles
        /// </summary>
        private ushort nProcessedTiles = 0;

        /// <summary>
        /// All tiles loaded event
        /// </summary>
        private readonly EventWaitHandle tilesLoadedEvent = new(false, EventResetMode.ManualReset);

        /// <summary>
        /// Horizontal center offset [px]
        /// </summary>
        private double xOffset { get; }
        /// <summary>
        /// Vertical center offset [px]
        /// </summary>
        private double yOffset { get; }

        /// <summary>
        /// Horizontal resolution [px]
        /// </summary>
        private int ResX { get; }
        /// <summary>
        /// Vertical resolution [px]
        /// </summary>
        private int ResY { get; }

        /// <summary>
        /// Rescale multiplier
        /// </summary>
        private int Scale { get; }

        /// <summary>
        /// Output tile absolute filepath
        /// </summary>
        private readonly string OutFname;

        /// <summary>
        /// Output tile image data
        /// </summary>
        public Bitmap? OutImage { get; private set; }


        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="wgsX">WGS84 X coord</param>
        /// <param name="wgsY">WGS84 Y coord</param>
        /// <param name="resX">Horizontal resolution</param>
        /// <param name="resY">Vertical resolution</param>
        /// <param name="scale">Rescale value</param>
        /// <param name="zoom">Zoom level</param>
        public MapTile(double wgsX, double wgsY, int resX, int resY, int scale, byte zoom)
        {
            (int pxlX, int pxlY) = GetPixelFromWGS(wgsX, wgsY, zoom); //gets pixel coords

            //X variables
            double leftMargin = -(256 - (pxlX % 256)); //calculate pixels from tile left side
            int centerTileX = (int)Math.Ceiling((double)pxlX / 256); //get horizontal tile id
            xOffset = (256 - Math.Floor(((resX / 2) - leftMargin) % 256)) % 256; //calculate horizontal offset in px

            int nLeftTiles = (int)Math.Ceiling(((resX / 2) - leftMargin + xOffset) / 256); //number of more tiles needed on left side
            int nRightTiles = (int)Math.Ceiling(((resX / 2) - (256 - leftMargin)) / 256); //number of more tiles needed on right side

            SourceTiles.nTiles horizontalTiles = new(centerTileX, nLeftTiles, nRightTiles); //build horizontal nTiles

            //Y variables
            double topMargin = -(256 - (pxlY % 256)); //calculate pixels from top side
            int centerTileY = (int)Math.Ceiling((double)pxlY / 256); //get vertical tile id
            yOffset = (256 - Math.Floor(((resY / 2) - topMargin) % 256)) % 256; //calculate vertical offset in px

            int nTopTiles = (int)Math.Ceiling(((resY / 2) - topMargin + yOffset) / 256); //number of more tiles needed on upper side
            int nBotTiles = (int)Math.Ceiling(((resY / 2) - (256 - topMargin)) / 256); //number of more tiles needed on bottom side

            SourceTiles.nTiles verticalTiles = new(centerTileY, nTopTiles, nBotTiles); //build vertical nTiles

            Zoom = zoom;
            Mapset = App.Mapsets[Settings.Default.Mapset];
            SourceTiles = new SourceTiles(horizontalTiles, verticalTiles);
            OutFname = Path.Join(OutCache, $"{Mapset.Value}_{Zoom}_{string.Format("{0:N6}", wgsX).Replace('.', '_')}-{string.Format("{0:N6}", wgsY).ToString().Replace('.', '_')}.jpg");

            ResX = resX;
            ResY = resY;
            Scale = scale;

            Utils.Log($"TILE -> Creating tile {OutFname}", Utils.LOG_LEVEL.VERBOSE);
        }

        /// <summary>
        /// Builds output map tile.
        /// </summary>
        /// <returns>Absolute filepath to map tile.</returns>
        public string Get()
        {
            if (File.Exists(OutFname) && Settings.Default.Cache) //check if tile is cached
            {
                OutImage = new Bitmap(OutFname); //returned cached version
                Utils.Log($"TILE -> Found cached image for {OutFname}", Utils.LOG_LEVEL.VERBOSE);
            }
            else
            {
                Utils.Log($"TILE -> Generating new image {OutFname}", Utils.LOG_LEVEL.VERBOSE);
                Bitmap _outImage = new(ResX, ResY); //create new image

                LoadSourceTiles(); //(down)load source tiles
                tilesLoadedEvent.WaitOne(); //wait until all source tiles are ready
                Utils.Log("TILE -> All required source tiles fetched", Utils.LOG_LEVEL.VERBOSE);

                //paste each tile inside newly created image
                using (Graphics g = Graphics.FromImage(_outImage))
                {
                    for (int y = 0; y < SourceTiles.yTiles.Count; y++)
                    {
                        for (int x = 0; x < SourceTiles.xTiles.Count; x++)
                        {
                            if (SourceTiles.Data[x, y] == null)
                                continue;

                            try
                            {
                                using MemoryStream ms = new(SourceTiles.Data[x, y]);
                                g.DrawImage(new Bitmap(ms), new Rectangle((x * 256) - (int)xOffset, (y * 256) - (int)yOffset, 256, 256));
                            }
                            catch { }
                        }
                    }
                }
                Utils.Log($"TILE -> Image {OutFname} succesfully generated", Utils.LOG_LEVEL.VERBOSE);

                //rescale created image if needed
                OutImage = Scale != 1 ? new Bitmap(_outImage, new Size(ResX * Scale, ResY * Scale)) : _outImage;
                if (Scale != 1)
                    Utils.Log($"TILE -> Rescaling generated image by {Scale}", Utils.LOG_LEVEL.VERBOSE);

                if (!Settings.Default.Cache)
                    return OutFname;

                Utils.Log($"TILE -> Flushing image to file", Utils.LOG_LEVEL.VERBOSE);
                Directory.CreateDirectory(OutCache);
                OutImage.Save(OutFname, ImageFormat.Jpeg);
            }
            return OutFname;
        }

        /// <summary>
        /// Downloads source map tile.
        /// </summary>
        /// <param name="fname">Requested source tile filename</param>
        /// <param name="fpath">Target source tile absolute filepath</param>
        /// <param name="x">Horizontal position of tile inside output tile</param>
        /// <param name="y">Vertical position of tile inside output tile</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private async Task DownloadImage(string fname, string fpath, int x, int y)
        {
            string requestUrl = $"https://mapserver.mapy.cz/{Mapset.Value}/{fname}";
            HttpResponseMessage response = await App.DownloadClient.GetAsync(requestUrl);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                Utils.Log($"FETCH -> Failed to download image {requestUrl}", Utils.LOG_LEVEL.VERBOSE);

            byte[] data = await response.Content.ReadAsByteArrayAsync();
            SourceTiles.Data[x, y] = data;

            if (Settings.Default.Cache)
                await File.WriteAllBytesAsync(fpath, data); //HACK ME: check if we have to wait for file to be saved
        }

        /// <summary>
        /// Downloads or loads cached source map tiles needed for output tile.
        /// </summary>
        private void LoadSourceTiles()
        {
            try
            {
                if (Settings.Default.Cache)
                    Directory.CreateDirectory(SourceCache);
            }
            catch (Exception e)
            {
                Settings.Default.Cache = false;
                Settings.Default.Save();
                App.MW?.Dispatcher.Invoke(() => App.MW.cachingCheckbox.IsChecked = false);
                Utils.Log($"FETCH -> Failed to create cache directory - disabling caching:{Environment.NewLine}{e}", Utils.LOG_LEVEL.ERROR);
            }

            Parallel.For(SourceTiles.xTiles.CenterTile - SourceTiles.xTiles.nTilesBefore, SourceTiles.xTiles.CenterTile + SourceTiles.xTiles.nTilesAfter + 1, (x) =>
            {
                Parallel.For(SourceTiles.yTiles.CenterTile - SourceTiles.yTiles.nTilesBefore, SourceTiles.yTiles.CenterTile + SourceTiles.yTiles.nTilesAfter + 1, (y) =>
                {
                    string fname = $"{Zoom}-{x}-{y}";
                    string fpath = Path.Join(SourceCache, $"{Mapset.Value}_{fname}.jpg");

                    Task.Run(async () =>
                    {
                        try
                        {
                            int lx = x - SourceTiles.xTiles.CenterTile + SourceTiles.xTiles.nTilesBefore;
                            int ly = y - SourceTiles.yTiles.CenterTile + SourceTiles.yTiles.nTilesBefore;

                            if (File.Exists(fpath) && Settings.Default.Cache) //try to load cached version if available
                            {
                                SourceTiles.Data[lx, ly] = await File.ReadAllBytesAsync(fpath);
                            }
                            else //download fresh tile otherwise
                            {
                                await DownloadImage(fname, fpath, lx, ly);
                            }
                        }
                        catch (Exception e) { Utils.Log($"FETCH -> Failed to fetch source tile {Mapset.Value}_{fname}: {e}", Utils.LOG_LEVEL.ERROR); }

                        lock (tilesLoadedEvent)
                        {
                            nProcessedTiles++; //increment loaded tiles

                            if (nProcessedTiles == SourceTiles.xTiles.Count * SourceTiles.yTiles.Count) //if last tile loaded, set event
                                tilesLoadedEvent.Set();
                        }
                    });
                });
            });
        }
    }
}
