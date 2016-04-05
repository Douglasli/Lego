using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gqqnbig
{
    static class Math
    {
        /// <summary>
        /// 将一个角度转换为[0,360)。
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double WrapAngle360(double angle)
        {
            angle = angle % 360;
            if (angle < 0)
                angle += 360;

            return angle;
        }
    }
}
