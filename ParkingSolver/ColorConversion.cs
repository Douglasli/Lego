using System.Diagnostics.Contracts;
using System.Windows.Media;

namespace Gqqnbig.Drawing
{
    internal static partial class ColorConversion
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="h">[0,360]</param>
        /// <param name="s">[0,1]</param>
        /// <param name="l">[0,1]</param>
        /// <returns></returns>
        public static Color HslToRgb(double h, double s, double l)
        {
            Contract.Requires(0 <= h && h <= 360);
            Contract.Requires(0 <= s && s <= 1);
            Contract.Requires(0 <= l && l <= 1);

            double b;
            double g;
            double r;
            if (s == 0)
                r = g = b = l;
            else
            {
                double q;
                if (l < 0.5)
                    q = l * (1 + s);
                else
                    q = l + s - l * s;

                double p = 2 * l - q;
                double hk = h / 360;

                double[] t = { hk + 1 / 3d, hk, hk - 1 / 3d };

                for (int i = 0; i < 3; i++)
                {
                    if (t[i] < 0)
                        t[i] += 1.0;
                    if (t[i] > 1)
                        t[i] -= 1.0;

                    if (t[i] < 1 / 6d)
                        t[i] = p + ((q - p) * 6.0 * t[i]);
                    else if (t[i] < 0.5) //(1.0/6.0)<=T[i] && T[i]<0.5  
                        t[i] = q;
                    else if (t[i] < 2 / 3d) // 0.5<=T[i] && T[i]<(2.0/3.0)  
                        t[i] = p + (q - p) * 6 * ((2 / 3d) - t[i]);
                    else
                        t[i] = p;
                }
                r = t[0];
                g = t[1];
                b = t[2];
            }

            Color color = Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
            return color;
        }
    }
}
