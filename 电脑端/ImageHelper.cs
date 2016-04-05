using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.Lego
{
    static class ColorExtensions
    {
        /// <summary>
        /// 两个颜色的RGB三个通道值是否在threshold指定的范围内。
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static bool IsSimilar(this Color c1, Color c2, int threshold)
        {
            return Math.Abs(c1.R - c2.R) <= threshold
                && Math.Abs(c1.G - c2.G) <= threshold
                && Math.Abs(c1.B - c2.B) <= threshold;

        }

        /// <summary>
        /// 两个颜色的RGB三个通道值是否在threshold指定的范围内。
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <param name="thresholds"></param>
        /// <returns></returns>
        public static bool IsSimilar(this Bgr c1, Bgr c2, int threshold1, int threshold2, int threshold3) //where TColor : IColor
        {
            //if (thresholds.Length != c1.Dimension)
            //    throw new ArgumentException("必须指定" + c1.Dimension + "个thresholds参数");
            //if (c1.Dimension != c2.Dimension)
            //    throw new ArgumentException();

            if (Math.Abs(c1.MCvScalar.v0 - c2.MCvScalar.v0) > threshold1)
                return false;
            if (Math.Abs(c1.MCvScalar.v1 - c2.MCvScalar.v1) > threshold2)
                return false;
            if (Math.Abs(c1.MCvScalar.v2 - c2.MCvScalar.v2) > threshold3)
                return false;
            //if (c1.Dimension > 3 && Math.Abs(c1.MCvScalar.v3 - c2.MCvScalar.v3) > thresholds)
            //    return false;
            return true;

        }

    }
}
