using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
            public int Count { get => nTilesBefore + 1 + nTilesAfter; }

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
            uint world_size = (uint)Math.Pow(2, (zoom + 8));

            double f = Math.Min(Math.Max(Math.Sin(y * Math.PI / 180), -0.9999), 0.9999);

            double retX = (x + 180) / 360 * world_size;
            double retY = (1 - 0.5 * Math.Log((1 + f) / (1 - f)) / Math.PI) / 2 * world_size;

            return (Convert.ToInt32(retX), Convert.ToInt32(retY));
        }

        /// <summary>
        /// Source map tiles cache
        /// </summary>
        public static string SourceCache { get => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapyCZforTS", "source_cache"); }

        /// <summary>
        /// Output map tiles cache
        /// </summary>
        public static string OutCache { get => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapyCZforTS", "output_cache"); }

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
        public string Mapset { get; }

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
            double leftMargin = pxlX % 256; //calculate pixels from tile left side
            int centerTileX = (int)Math.Ceiling((double)pxlX / 256); //get horizontal tile id
            xOffset = (256 - Math.Floor((resX / 2 - leftMargin) % 256)) % 256; //calculate horizontal offset in px

            int nLeftTiles = (int)Math.Ceiling((resX / 2 - leftMargin + xOffset) / 256); //number of more tiles needed on left side
            int nRightTiles = (int)Math.Ceiling((resX / 2 - (256 - leftMargin)) / 256); //number of more tiles needed on right side

            SourceTiles.nTiles horizontalTiles = new(centerTileX, nLeftTiles, nRightTiles); //build horizontal nTiles

            //Y variables
            double topMargin = pxlY % 256; //calculate pixels from top side
            int centerTileY = (int)Math.Ceiling((double)pxlY / 256); //get vertical tile id
            yOffset = (256 - Math.Floor((resY / 2 - topMargin) % 256)) % 256; //calculate vertical offset in px

            int nTopTiles = (int)Math.Ceiling((resY / 2 - topMargin + yOffset) / 256); //number of more tiles needed on upper side
            int nBotTiles = (int)Math.Ceiling((resY / 2 - (256 - topMargin)) / 256); //number of more tiles needed on bottom side

            SourceTiles.nTiles verticalTiles = new(centerTileY, nTopTiles, nBotTiles); //build vertical nTiles

            Zoom = zoom;
            Mapset = App.Mapset;
            SourceTiles = new SourceTiles(horizontalTiles, verticalTiles);
            OutFname = Path.Join(OutCache, $"{Mapset}_{Zoom}_{string.Format("{0:N6}", wgsX).Replace('.', '_')}-{string.Format("{0:N6}", wgsY).ToString().Replace('.', '_')}.jpg");

            ResX = resX;
            ResY = resY;
            Scale = scale;
        }

        /// <summary>
        /// Builds output map tile.
        /// </summary>
        /// <returns>Absolute filepath to map tile.</returns>
        public string Get()
        {
            if (File.Exists(OutFname) && App.Cache) //check if tile is cached
            {
                OutImage = new Bitmap(OutFname); //returned cached version
            }
            else
            {
                Bitmap _outImage = new(ResX, ResY); //create new image

                LoadSourceTiles(); //(down)load source tiles
                tilesLoadedEvent.WaitOne(); //wait until all source tiles are ready

                //paste each tile inside newly created image
                using (Graphics g = Graphics.FromImage(_outImage))
                {
                    for (int y = 0; y < SourceTiles.yTiles.Count; y++)
                    {
                        for (int x = 0; x < SourceTiles.xTiles.Count; x++)
                        {
                            if (SourceTiles.Data[x, y] == null)
                                continue;

                            using MemoryStream ms = new(SourceTiles.Data[x, y]);
                            g.DrawImage(new Bitmap(ms), new Point(x * 256 - (int)xOffset, y * 256 - (int)yOffset));
                        }
                    }
                }

                //rescale created image if needed
                if (Scale != 1)
                    OutImage = new Bitmap(_outImage, new Size(ResX * Scale, ResY * Scale));
                else
                    OutImage = _outImage;

                if (!App.Cache)
                    return OutFname;

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
            var response = await App.DownloadClient.GetAsync($"https://mapserver.mapy.cz/{Mapset}/{fname}");

            byte[] data = await response.Content.ReadAsByteArrayAsync();
            SourceTiles.Data[x, y] = data;

            if (App.Cache)
                await File.WriteAllBytesAsync(fpath, data); //HACK ME: check if we have to wait for file to be saved
        }

        /// <summary>
        /// Downloads or loads cached source map tiles needed for output tile.
        /// </summary>
        private void LoadSourceTiles()
        {
            if (App.Cache)
                Directory.CreateDirectory(SourceCache);

            Parallel.For(SourceTiles.xTiles.CenterTile - SourceTiles.xTiles.nTilesBefore, SourceTiles.xTiles.CenterTile + SourceTiles.xTiles.nTilesAfter + 1, (x) =>
            {
                Parallel.For(SourceTiles.yTiles.CenterTile - SourceTiles.yTiles.nTilesBefore, SourceTiles.yTiles.CenterTile + SourceTiles.yTiles.nTilesAfter + 1, (y) =>
                {
                    string fname = $"{Zoom}-{x}-{y}.jpg";
                    string fpath = Path.Join(SourceCache, $"{Mapset}_{fname}");

                    Task.Run(async () =>
                    {
                        try
                        {
                            int lx = x - SourceTiles.xTiles.CenterTile + SourceTiles.xTiles.nTilesBefore;
                            int ly = y - SourceTiles.yTiles.CenterTile + SourceTiles.yTiles.nTilesBefore;

                            if (File.Exists(fpath) && App.Cache) //try to load cached version if available
                            {
                                SourceTiles.Data[lx, ly] = await File.ReadAllBytesAsync(fpath);
                            }
                            else //download fresh tile otherwise
                            {
                                await DownloadImage(fname, fpath, lx, ly);
                            }
                        }
                        catch { }

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
