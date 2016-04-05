using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV;
using Emgu.CV.Structure;
using Gqqnbig.Lego;
using PointInt = System.Drawing.Point;

namespace OpenCV测试
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Image<Gray, byte> backgroundImage;
        private Image<Bgr, byte> image;

        public MainWindow()
        {
            InitializeComponent();

            DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(Slider.ValueProperty, typeof(Slider));
            descriptor.AddValueChanged(cannyThresholdSlider, argumentChanged);
            descriptor.AddValueChanged(cannyThresholdLinkingSlider, argumentChanged);
            descriptor.AddValueChanged(thresholdSlider, argumentChanged);
            descriptor.AddValueChanged(minLineWidthSlider, argumentChanged);
            descriptor.AddValueChanged(gapBetweenLinesSlider, argumentChanged);


            image =new Image<Bgr, byte>(@"C:\Users\loveright\Documents\Visual Studio 2013\Projects\乐高\电脑端\bin\Debug\images\backgroundImage.bmp");
            backgroundImage=new Image<Gray, byte>(@"C:\Users\loveright\Documents\Visual Studio 2013\Projects\乐高\电脑端\bin\Debug\images\backgroundImage.bmp");

            ////设定地面区域
            //int left = image.Width / 4;
            //int width = image.Width / 2;
            //int top = image.Height / 16 * 13;
            //int height = image.Height / 8;

            //int threshold = 3;
            //Stack<PointInt> connectedPoints = new Stack<PointInt>();
            ////选区内是黑色，选区外是白色。
            //Gray selectionInColor = new Gray(255);
            //Gray selectionOutColor = new Gray(0);
            //backgroundImage = new Image<Gray, byte>(image.Width, image.Height, selectionOutColor);
            ////加入
            //for (int x = left; x < left + width; x++)
            //    for (int y = top; y < top + height; y++)
            //        backgroundImage[y, x] = selectionInColor;
            //for (int x = left; x < left + width; x++)
            //{
            //    connectedPoints.Push(new PointInt(x, top));
            //    connectedPoints.Push(new PointInt(x, top + height));
            //}

            //for (int y = top; y < top + height; y++)
            //{
            //    connectedPoints.Push(new PointInt(left, y));
            //    connectedPoints.Push(new PointInt(left + width, y));
            //}

            //while (connectedPoints.Count > 0)
            //{
            //    PointInt p = connectedPoints.Pop();

            //    if (backgroundImage[p].Equals(selectionInColor))
            //        continue;

            //    backgroundImage[p] = selectionInColor;

            //    bool isBorder = false;
            //    Bgr color = image[p];
            //    if (p.X > 0 && image[p.Y, p.X - 1].IsSimilar(color, threshold))
            //        connectedPoints.Push(new PointInt(p.X - 1, p.Y));
            //    else
            //        isBorder = true;
            //    if (p.X < image.Width - 1 && image[p.Y, p.X + 1].IsSimilar(color, threshold))
            //        connectedPoints.Push(new PointInt(p.X + 1, p.Y));
            //    else
            //        isBorder = true;
            //    if (p.Y > 0 && image[p.Y - 1, p.X].IsSimilar(color, threshold))
            //        connectedPoints.Push(new PointInt(p.X, p.Y - 1));
            //    else
            //        isBorder = true;
            //    if (p.Y < image.Height - 1 && image[p.Y + 1, p.X].IsSimilar(color, threshold))
            //        connectedPoints.Push(new PointInt(p.X, p.Y + 1));
            //    else
            //        isBorder = true;
            //}

            //backgroundImage._Dilate(3);
            //backgroundImage._Erode(3);


            ////image.Draw(new System.Drawing.Rectangle(left, top, width, height), new Bgr(0, 0, 255), 1);
            ////CvInvoke.cvShowImage("", image);


            //CvInvoke.cvShowImage("backgroundImage", backgroundImage);


        }

        private void argumentChanged(object sender, EventArgs e)
        {
            //cannyThresh控制强边缘的初始分割，即如果一个像素的梯度大与上限值，则被认为是边缘像素，如果小于下限阈值，则被抛弃。
            //threshLinking控制边缘连接
            //CvInvoke.cvShowImage("Canny", backgroundImage.Canny(50, 100));
            //LineSegment2D[][] lines = backgroundImage.HoughLines(cannyThresholdSlider.Value, cannyThresholdLinkingSlider.Value,
            //    1, System.Math.PI / 180,
            //    (int)thresholdSlider.Value, minLineWidthSlider.Value, gapBetweenLinesSlider.Value);

            LineSegment2D[][] lines = backgroundImage.HoughLines(50, 50, 1, Math.PI / 180, 15, 3, 25);


            var newImage = image.Copy();

            foreach (LineSegment2D line in lines[0])
                newImage.Draw(line, new Bgr(255, 0, 255), 2);
            //CvInvoke.cvShowImage("line", image);

            this.imageControl.Source = Gqqnbig.Lego.EmguCVInWpf.ToBitmapSource(newImage);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            argumentChanged(sender, e);
        }



    }
}
