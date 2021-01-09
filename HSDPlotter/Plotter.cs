using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;

namespace HSDPlotter
{
    class Plotter
    {

        private static double Normalize(double val, double min, double max)
        {
            if (double.IsNaN(val))
            {
                return 0;
            }
            if (val < min)
            {
                return 0;
            }
            else if (val > max)
            {
                return 1;
            }
            var spread = max - min;
            return (val - min) / spread;
        }

        private static void FindTABound(float[,] coord, ref float min, ref float max)
        {
            // Checking four vertices
            var xdmax = coord.GetLength(0);
            var ydmax = coord.GetLength(1);
            var coord_ul = coord[0, 0];
            var coord_ur = coord[0, ydmax - 1];
            var coord_ll = coord[xdmax - 1, 0];
            var coord_lr = coord[xdmax - 1, ydmax - 1];
            var tmp_arr = new[] { coord_ul, coord_ur, coord_ll, coord_lr };
            min = tmp_arr.Min();
            max = tmp_arr.Max();
        }
        public static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                //return;
            }
            //var filepath = args[0];
            var filepath = @"F:\Data\Satellite\HIMAWARI-8\HSD\HS_H08_20181001_2100_B13_R301_R20_S0101.DAT.bz2";
            var fobj = new HSDReader(filepath);
            var data = fobj.data;
            float[,] lon = new float[fobj.nline, fobj.ncol];
            float[,] lat = new float[fobj.nline, fobj.ncol];
            fobj.GetLonLat(ref lon, ref lat);
            int xcount = 0, ycount = 0;
            float xmin = 0, xmax = 0, ymin = 0, ymax = 0;
            FindTABound(lon, ref xmin, ref xmax);
            FindTABound(lat, ref ymin, ref ymax);
            var remap_data = Projection.Resample(lon, lat, data,
                new[] {xmin, xmax, ymin, ymax }, ref xcount, ref ycount);
            var image1 = new Bitmap(xcount, ycount);
            int x, y;
            for (x = 0; x < xcount; x++)
            {
                for (y = 0; y < ycount; y++)
                {
                    var grey = 1 - Normalize(remap_data[x, y], -100, 50);
                    int rgb = (int)(grey * 255);
                    Color color = Color.FromArgb(rgb, rgb, rgb);
                    image1.SetPixel(x, ycount - y - 1, color);
                }
            }
            string path = Path.GetDirectoryName(filepath);
            string fname = Path.GetFileName(filepath).Split('.')[0];
            fname += ".png";
            string save_path = Path.Combine(path, fname);
            image1.Save(save_path, ImageFormat.Png);
        }
    }
}
