using System;
using System.Diagnostics;

namespace Gqqnbig.Lego
{
    public class PhoneService : IPhoneService //不能继承自ClientBase<IPhoneService>！！这是在客户端才写的！！！
    {
        private static long lastSeqeuenceNumber = 0;
        private static int width;
        private static int height;
        private static long timeShift;

        public static event EventHandler<ImageReceivedEventArgs> ImageReceived;

        public void StartSession(long seq, int width, int height)
        {
            lastSeqeuenceNumber = seq;
            PhoneService.width = width;
            PhoneService.height = height;

            timeShift = DateTime.UtcNow.GetUnixTimestamp() - seq;


            Debug.WriteLine("timeShift=" + timeShift);
        }

        public void SendImage(long seq, byte[] image)
        {
            if (seq > lastSeqeuenceNumber && ImageReceived != null)
            {
                ImageReceived(this, new ImageReceivedEventArgs
                {
                    ImageData = image,
                    Tick = seq + timeShift,
                    Width = width,
                    Height = height
                });
                lastSeqeuenceNumber = seq;
            }
        }
    }


    public class TimedEventArgs : EventArgs
    {
        /// <summary>
        /// 表示在何时发生此事件。
        /// <para>当前的UTC时间与1970年1月1日的毫秒数。</para>
        /// </summary>
        public long Tick { get; set; }
    }

    public class MotorSpeedChangedEventArgs : TimedEventArgs
    {
        public double LeftSpeed { get; set; }

        public double RightSpeed { get; set; }
    }

    public class ImageReceivedEventArgs : TimedEventArgs
    {
        public byte[] ImageData { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }
}