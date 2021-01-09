using System.IO;
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
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }
            var filepath = args[0];
            var fobj = new HSDReader(filepath);
            var data = fobj.data;
            var image1 = new Bitmap(fobj.ncol, fobj.nline);
            int x, y;
            for (x = 0; x < image1.Width; x++)
            {
                for (y = 0; y < image1.Height; y++)
                {
                    var idx = fobj.ncol * y + x;
                    var grey = 1 - Normalize(data[idx], -100, 50);
                    int rgb = (int)(grey * 255);
                    Color color = Color.FromArgb(rgb, rgb, rgb);
                    image1.SetPixel(x, y, color);
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
