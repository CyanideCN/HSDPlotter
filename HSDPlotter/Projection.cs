using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KdTree;
using KdTree.Math;

namespace HSDPlotter
{
    class Projection
    {

        Func<double[], double[], double> L2Norm = (x, y) =>
        {
            double dist = 0;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }
            return dist;
        };
        public static float[,] Resample(float[,] x, float[,] y, float[,] data, float[] bounds,
            ref int x_count, ref int y_count, double step = 0.1)
        {
            // TODO: Rewrite logic
            var tree = new KdTree<float, float>(2, new GeoMath());
            int xdim = x.GetLength(0);
            int ydim = x.GetLength(1);
            for (int i = 0; i < xdim; i++)
            {
                for (int j = 0; j < ydim; j++)
                {
                    tree.Add(new[] { x[i, j], y[i, j] }, data[i, j]);
                }
            }
            tree.Balance();
            var x_range = bounds[1] - bounds[0];
            var y_range = bounds[3] - bounds[2];
            x_count = (int)(x_range / step);
            y_count = (int)(y_range / step);
            float[,] remapped = new float[x_count, y_count];
            for (int i = 0; i < x_count; i++)
            {
                for (int j = 0; j < y_count; j++)
                {
                    //Console.WriteLine(i.ToString() + "," + j.ToString());
                    float xcor = (float)(bounds[0] + i * step);
                    float ycor = (float)(bounds[2] + j * step);
                    var nodes = tree.GetNearestNeighbours(new[] { xcor, ycor }, 1);
                    var result = nodes.Select(n => n.Value).ToArray();
                    var coords = nodes.Select(n => n.Point).ToArray();
                    if (result.Length == 0)
                    {
                        remapped[i, j] = float.NaN;
                    }
                    else
                    {
                        //Console.WriteLine(i.ToString() + "," + j.ToString());
                        var xy = coords[0];
                        var dist = Math.Pow(xy[0] - xcor, 2) + Math.Pow(xy[1] - ycor, 2);
                        if (dist > 0.05)
                        {
                            remapped[i, j] = float.NaN;
                        }
                        else
                        {
                            remapped[i, j] = result[0];
                        }
                    }
                }
            }
            return remapped;
        }
    }
}
