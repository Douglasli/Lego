using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApplication1
{
    partial class Math2D
    {

        /// <summary>
        /// 获取直线或线段到点的距离
        /// </summary>
        /// <param name="l1">直线上的一点</param>
        /// <param name="l2">直线上的一点</param>
        /// <param name="p"></param>
        /// <param name="isSegment">是直线还是线段</param>
        /// <returns></returns>
        public static double GetLineToPointDistance(Point l1, Point l2, Point p, bool isSegment)
        {

            double dist = Vector.CrossProduct(l2 - l1, p - l1) / (l1 - l2).Length;
            if (isSegment)
            {
                double dot1 = Vector.Multiply(l2 - l1, p - l2);
                if (dot1 > 0)
                    return (l2 - p).Length;

                double dot2 = Vector.Multiply(l1 - l2, p - l1);
                if (dot2 > 0)
                    return (l1 - p).Length;
            }
            return Math.Abs(dist);
        }
    }
}
