using System;
using System.Collections.Generic;
using System.Linq;

namespace Gqqnbig.Linq
{
    /*
     * 单文件不要设定访问性。默认访问性为internal。
     * 要设置为partial，以便项目内可以设置同名分部类，更改访问性。
     */

    static partial class Linq
    {
        public static double Variance(this IEnumerable<double> numbers)
        {
            var sum = numbers.Sum();
            var count = numbers.Count();


            return (from n in numbers
                    select System. Math.Pow((n - sum / count), 2)).Sum() / count;
        }

        /// <summary>
        /// 计算此序列的方差。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static double Variance<T>(this IEnumerable<T> elements, Func<T, int> selector)
        {
            var sum = elements.Sum(selector);
            var count = elements.Count();


            return (from n in elements
                    select System.Math.Pow((selector(n) - sum / count), 2)).Sum() / count;
        }

        /// <summary>
        /// 已知平均数，计算方差。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <param name="mean"> </param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static double Variance<T>(this IEnumerable<T> elements, double mean, Func<T, int> selector)
        {
            return (from n in elements
                    select System.Math.Pow((selector(n) - mean), 2)).Sum() / elements.Count();
        }

        /// <summary>
        /// 计算中位数。
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static double Median(this IEnumerable<double> source)
        {
            if (!source.Any())
            {
                throw new InvalidOperationException("Cannot compute median for an empty set.");
            }

            var sortedList = from number in source
                             orderby number
                             select number;

            var count = sortedList.Count();
            int itemIndex = count / 2;

            if (count % 2 == 0)
            {
                // Even number of items.
                return (sortedList.ElementAt(itemIndex) + sortedList.ElementAt(itemIndex - 1)) / 2;
            }
            else
            {
                // Odd number of items.
                return sortedList.ElementAt(itemIndex);
            }
        }
    }
}
