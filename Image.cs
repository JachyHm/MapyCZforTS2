using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MapyCZforTS_CS
{
    public class SourceTiles
    {
        public class nTiles
        {
            public int CenterTile { get; }
            public int nTilesBefore { get; }
            public int nTilesAfter { get; }

            public int count { get => nTilesBefore + 1 + nTilesAfter; }

            public nTiles(int centerTile, int nTilesBefore, int nTilesAfter)
            {
                CenterTile = centerTile;
                this.nTilesBefore = nTilesBefore;
                this.nTilesAfter = nTilesAfter;
            }
        }

        public nTiles xTiles { get; }
        public nTiles yTiles { get; }

        public byte[,][] Data { get; }

        public SourceTiles(nTiles xTiles, nTiles yTiles)
        {
            this.xTiles = xTiles;
            this.yTiles = yTiles;
            Data = new byte[xTiles.count, yTiles.count][];
        }
    }

    internal class Image
    {
        public static (int, int) GetPixelFromWGS(double x, double y, byte zoom)
        {
            uint world_size = (uint)Math.Pow(2,(zoom + 8));

            double f = Math.Min(Math.Max(Math.Sin(y * Math.PI / 180), -0.9999), 0.9999);

            double retX = (x + 180) / 360 * world_size;
            double retY = (1 - 0.5 * Math.Log((1 + f) / (1 - f)) / Math.PI) / 2 * world_size;

            return (Convert.ToInt32(retX), Convert.ToInt32(retY));
        }

        public static string SourceCache { get => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapyCZforTS", "source_cache"); }
        public static string OutCache { get => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MapyCZforTS", "output_cache"); }

        private SourceTiles SourceTiles { get; }

        public byte Zoom { get; }
        public string Mapset { get; }

        private ushort nProcessedTiles = 0;
        private readonly EventWaitHandle tilesLoadedEvent = new(false, EventResetMode.ManualReset);

        private double xOffset { get; }
        private double yOffset { get; }

        private int ResX { get; }
        private int ResY { get; }

        private int Scale { get; }

        private readonly string OutFname;

        public System.Drawing.Bitmap? OutImage { get; private set; }

        public Image(double wgsX, double wgsY, int resX, int resY, int scale, byte zoom)
        {
            (int pxlX, int pxlY) = GetPixelFromWGS(wgsX, wgsY, zoom);

            //X variables
            double leftMargin = pxlX%256;
            int centerTileX = (int)Math.Ceiling((double)pxlX/256);
            xOffset = (256 - Math.Floor((resX / 2 - leftMargin) % 256)) % 256;

            int nLeftTiles = (int)Math.Ceiling((resX/2 - leftMargin + xOffset)/256);
            int nRightTiles = (int)Math.Ceiling((resX/2 - (256 - leftMargin))/256);

            SourceTiles.nTiles horizontalTiles = new(centerTileX, nLeftTiles, nRightTiles);

            //Y variables
            double topMargin = pxlY%256;
            int centerTileY = (int)Math.Ceiling((double)pxlY/256);
            yOffset = (256 - Math.Floor((resY / 2 - topMargin) % 256)) % 256;

            int nTopTiles = (int)Math.Ceiling((resY/2 - topMargin + yOffset)/256);
            int nBotTiles = (int)Math.Ceiling((resY/2 - (256 - topMargin))/256);

            SourceTiles.nTiles verticalTiles = new(centerTileY, nTopTiles, nBotTiles);

            Zoom = zoom;
            Mapset = App.Mapset;
            SourceTiles = new SourceTiles(horizontalTiles, verticalTiles);
            OutFname = Path.Join(OutCache, $"{Mapset}_{Zoom}_{wgsX.ToString().Replace('.', '_')}-{wgsY.ToString().Replace('.', '_')}.jpg");

            ResX = resX;
            ResY = resY;
            Scale = scale;
        }

        public string Get()
        {
            System.Drawing.Bitmap _outImage = new(ResX, ResY);

            PrepareImages();
            tilesLoadedEvent.WaitOne();

            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(_outImage))
            {
                for (int y = 0; y < SourceTiles.yTiles.count; y++)
                {
                    for (int x = 0; x < SourceTiles.xTiles.count; x++)
                    {
                        if (SourceTiles.Data[x, y] == null)
                            continue;

                        using MemoryStream ms = new(SourceTiles.Data[x, y]);
                        g.DrawImage(new System.Drawing.Bitmap(ms), new System.Drawing.Point(x * 256 - (int)xOffset, y * 256 - (int)yOffset));
                    }
                }
            }

            Directory.CreateDirectory(OutCache);
            OutImage = new System.Drawing.Bitmap(_outImage, new System.Drawing.Size(ResX * Scale, ResY * Scale));
            OutImage.Save(OutFname, System.Drawing.Imaging.ImageFormat.Jpeg);

            return OutFname;
        }

        private async Task DownloadImage(string fname, int x, int y)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync($"https://mapserver.mapy.cz/{Mapset}/{fname}");
            SourceTiles.Data[x, y] = await response.Content.ReadAsByteArrayAsync();
        }

        private void PrepareImages()
        {
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
                            if (!File.Exists(fpath))
                            {
                                int lx = x - SourceTiles.xTiles.CenterTile + SourceTiles.xTiles.nTilesBefore;
                                int ly = y - SourceTiles.yTiles.CenterTile + SourceTiles.yTiles.nTilesBefore;
                                await DownloadImage(fname, lx, ly);
                            }
                            else
                            {
                                SourceTiles.Data[x, y] = await File.ReadAllBytesAsync(fpath);
                            }
                        }
                        catch { }

                        lock (tilesLoadedEvent)
                        {
                            nProcessedTiles++;

                            if (nProcessedTiles == SourceTiles.xTiles.count * SourceTiles.yTiles.count)
                                tilesLoadedEvent.Set();
                        }
                    });
                });
            });
        }
    }
}
