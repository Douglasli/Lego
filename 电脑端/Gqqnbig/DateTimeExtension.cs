using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gqqnbig
{
    public static class DateTimeExtension
    {
        private static readonly DateTime unixStartTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static long GetUnixTimestamp(this DateTime dateTime, bool isUtc = true)
        {
            if (isUtc)
                return Convert.ToInt64((dateTime - unixStartTime).TotalMilliseconds);
            else
                throw new ArgumentException("isUtc=false is not supported.");
        }
    }
}
