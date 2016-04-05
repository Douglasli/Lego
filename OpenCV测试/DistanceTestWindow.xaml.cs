using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Emgu.CV;
using Emgu.CV.Structure;
using PointInt = System.Drawing.Point;

namespace OpenCV测试
{
    /// <summary>
    /// DistanceTestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DistanceTestWindow : Window
    {
        Emgu.CV.Image<Bgr, byte> img = new Emgu.CV.Image<Bgr, byte>(@"C:\Users\loveright\Documents\Visual Studio 2013\Projects\乐高\电脑端\bin\Debug\images\13.jpg");

        const double cameraHeight = 9.5;

        public DistanceTestWindow()
        {
            InitializeComponent();

        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Image<Bgr, byte> image = this.img.Copy();
            Graphics g = Graphics.FromImage(image.Bitmap);

            Bgr lineColor = new Bgr(0, 0, 255);
            Font font = new Font("Arial", 16);

            for (int d = 20; d < image.Height / 2; d += 20)
            {
                double rd = GetRealVerticalDistaneFromPixelDistance(d);

                string text = rd.ToString("f0");

                g.DrawString(text, font, Brushes.Red, new PointF(0, image.Height / 2 + d - 8));

                image.Draw(new LineSegment2D(new PointInt(40, image.Height / 2 + d), new PointInt(image.Width - 40, image.Height / 2 + d)), lineColor, 1);
            }

            for (int realHorizontalDistance = 0; realHorizontalDistance < 100; realHorizontalDistance += 20)
            {
                //图像中点往下20像素。
                double realVerticalDistance1 = GetRealVerticalDistaneFromPixelDistance(20);
                //图像底部往上20像素。
                double realVerticalDistance2 = GetRealVerticalDistaneFromPixelDistance(image.Height / 2 - 20);

                double horizontalPixels1 = realHorizontalDistance * slider.Value / realVerticalDistance1;
                double horizontalPixels2 = realHorizontalDistance * slider.Value / realVerticalDistance2;

                image.Draw(new LineSegment2DF(new PointF((float)(image.Width / 2.0 - horizontalPixels1), image.Height / 2 + 20),
                    new PointF((float)(image.Width / 2.0 - horizontalPixels2), image.Height - 20)), lineColor, 1);

                string text = realHorizontalDistance.ToString();

                g.DrawString(text, font, Brushes.Red, new PointF((float)(image.Width / 2.0 - horizontalPixels1 - 8), image.Height / 2));
            }

            if (this.IsLoaded)
                imageControl.Source = Gqqnbig.Lego.EmguCVInWpf.ToBitmapSource(image);

        }

        double GetRealVerticalDistaneFromPixelDistance(double pixels)
        {

            return slider.Value * cameraHeight * 1 / pixels + 1.85864;
        }

        double GetRealHorizontalDistanceFromPixelDistance(double realVerticalDistance, double horizontalPixels)
        {
            return realVerticalDistance / slider.Value * horizontalPixels;
        }
    }
}
