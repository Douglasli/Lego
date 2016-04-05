using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.WindowsAPICodePack.Dialogs;
using Brushes = System.Windows.Media.Brushes;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using PointInt = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace Gqqnbig.Lego
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window
    {

        /// <summary>
        /// 相机成像距离，单位为像素。
        /// </summary>
        const double imagingDistance = 520.714;

        /// <summary>
        /// 相机距地面的高度，单位为厘米。
        /// </summary>
        const double cameraHeight = 9.4 + 0.8;

        ///// <summary>
        ///// 后轮中点的速度。
        ///// </summary>
        //public sbyte MidSpeed
        //{
        //    get { return (sbyte)GetValue(MidSpeedProperty); }
        //    set { SetValue(MidSpeedProperty, value); }
        //}

        public static readonly DependencyProperty MidSpeedProperty =
            DependencyProperty.Register("MidSpeed", typeof(sbyte), typeof(Window1), new PropertyMetadata((sbyte)0));



        /// <summary>
        /// 小于0是左，大于0是右。
        /// </summary>
        public double SteeringAngle
        {
            get { return (int)GetValue(SteeringAngleProperty); }
            set { SetValue(SteeringAngleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SteeringAngle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SteeringAngleProperty =
            DependencyProperty.Register("SteeringAngle", typeof(double), typeof(Window1), new PropertyMetadata(0d));


        //private const int maxRotationDegree = 80;

        /// <summary>
        /// 记录上次收到图像的时间
        /// </summary>
        private int lastImageTick = int.MaxValue;

        ///// <summary>
        ///// 记录上次转向的时间，转向2秒后方向盘放松。
        ///// </summary>
        //private int lastSteeringTick = int.MaxValue;

        private DispatcherTimer timer;

        //private Task getBackgroundTask;

        //Point startPosition;

        CameraVehicleBase vehicle = new CameraVehicle();


        //Bgr backgroundColorAverage;
        ///// <summary>
        ///// 背景色的标准差。
        ///// </summary>
        //MCvScalar backgroundColorDeviation;

        //double[][] backgroundPixelValues;
        //double[][] previousBackgroundPixelValues;

        Image<Gray, byte> backgroundImage;

        List<MotorSpeedChangedEventArgs> actionList;

        readonly BackgroundWorker analysisWorker;

        MotorSpeedChangedEventArgs lastMotorEvent;

        //VehicleState destVehicleState = null;

        /// <summary>
        /// 收集到几次背景。
        /// </summary>
        int collectedBackgroundCount = 0;


        public Window1()
        {
            InitializeComponent();

            //DependencyPropertyDescriptor.FromProperty(MidSpeedProperty, typeof(Window1)).AddValueChanged(this, MoveStateChanged);
            //DependencyPropertyDescriptor.FromProperty(SteeringAngleProperty, typeof(Window1)).AddValueChanged(this, MoveStateChanged);


            analysisWorker = new BackgroundWorker();
            analysisWorker.DoWork += AnalyzeFrame;

            vehicle.MotorSpeedChanged += CameraVehicle_MotorSpeedChanged;

        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            connectionComboBox.Items.Add("USB");
            foreach (string s in SerialPort.GetPortNames())
                connectionComboBox.Items.Add(s);

            var x = from n in NetworkInterface.GetAllNetworkInterfaces()
                    from add in n.GetIPProperties().UnicastAddresses
                    where n.OperationalStatus == OperationalStatus.Up
                         && (n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || n.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                         && add.Address.AddressFamily == AddressFamily.InterNetwork
                    select add.Address.ToString();

            interfaceComboBox.Items.Add("Select file");

            foreach (var ip in x)
                interfaceComboBox.Items.Add(ip);


            leftSteeringMaxTextBlock.Text = rightSteeringMaxTextBlock.Text = vehicle.State.TurnMaxDegree.ToString("f2");

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += timer_Tick;
            timer.Start();


            MapWindow w = GetMapWindow();
            w.map = new Map(300, 300);

            //w.carRectangle.Width = vehicle.State.Width;
            //w.carRectangle.Height = vehicle.State.Length;
            //w.carRectangle.SetValue(Canvas.LeftProperty, vehicle.State.Center.X - vehicle.State.Width / 2);
            //w.carRectangle.SetValue(Canvas.TopProperty, vehicle.State.Center.Y + vehicle.State.Length / 2);



            //backgroundImage = new Image<Gray, byte>(@"C:\Users\loveright\Documents\Visual Studio 2013\Projects\乐高\电脑端\bin\Debug\images\backgroundImage.bmp");
            //var img = new Image<Bgr, byte>(@"C:\Users\loveright\Documents\Visual Studio 2013\Projects\乐高\电脑端\bin\Debug\images\1425366576428.jpg");

            //AddToMap(backgroundImage, img);
        }

        private void AddToMap(Image<Gray, byte> backgroundImage, Image<Bgr, byte> img)
        {
            backgroundImage.Save("images/backgroundImage.bmp");

            MapWindow w = GetMapWindow();


            //因为已经是二值图像了，所以头两个参数的值不影响。
            LineSegment2D[] lines = backgroundImage.HoughLines(50, 50, 1, Math.PI / 180, 15, 3, 25)[0];

            Graphics g = Graphics.FromImage(img.Bitmap);
            foreach (LineSegment2D line in lines)
            {
                //在“天上”的东西不考虑。
                if (line.P1.Y < backgroundImage.Height / 2 || line.P2.Y < backgroundImage.Height / 2)
                    continue;

                //太远的东西不准确，不考虑。
                if (line.P1.Y < 240 || line.P2.Y < 240)
                    continue;

                double distanceInPixel = line.P1.Y - backgroundImage.Height / 2.0;
                double distanceInCM1 = GetRealVerticalDistaneFromPixelDistance(distanceInPixel);
                double hd1 = GetRealHorizontalDistanceFromPixelDistance(distanceInCM1, backgroundImage.Width / 2.0 - line.P1.X);

                distanceInPixel = line.P2.Y - backgroundImage.Height / 2.0;
                double distanceInCM2 = GetRealVerticalDistaneFromPixelDistance(distanceInPixel);
                double hd2 = GetRealHorizontalDistanceFromPixelDistance(distanceInCM2, backgroundImage.Width / 2.0 - line.P2.X);

                double k = Math.Abs((hd1 - hd2) / (distanceInCM1 - distanceInCM2));
                Font font = new Font("Arial", 12);
                if (k > 1.19) //看线是不是水平的
                {
                    double x1 = vehicle.State.Center.X + vehicle.CameraPosition.X - hd1;
                    double x2 = vehicle.State.Center.X + vehicle.CameraPosition.X - hd2;

                    //if ((x1 < 30 && x2 < 30 && distanceInCM1 > 30 && distanceInCM1 < 110) == false)
                    //{
                        w.map.Fixtures.Add(new System.Windows.Media.RectangleGeometry(new Rect(
                            Math.Min(x1, x2),
                            vehicle.State.Center.Y - vehicle.CameraPosition.Y - (distanceInCM1 + distanceInCM2) / 2,
                            Math.Abs(x2 - x1),
                            1)));
                    //}

                    img.Draw(line, new Bgr(255, 0, 0), 1);

                    float y = (line.P1.Y + line.P2.Y) / 2f;

                    g.DrawString("!" /*+ ((distanceInCM1 + distanceInCM2) / 2).ToString("f2")*/ + ",k=" + k.ToString("f2"), font, System.Drawing.Brushes.Red,
                        new PointF((line.P1.X + line.P2.X) / 2, y));
                }
                else if (k < 0.12) //线虽然在图像上是斜的，可能现实中是垂直的。
                {
                    g.DrawString("<>" /*+ hd1.ToString("f2")*/+ ",k=" + k.ToString("f2"), font, System.Drawing.Brushes.Red,
                        (new PointF((line.P1.X + line.P2.X) / 2, (line.P1.Y + line.P2.Y) / 2)));

                    img.Draw(line, new Bgr(255, 0, 0), 1);

                    double y1 = vehicle.State.Center.Y - vehicle.CameraPosition.Y - distanceInCM1;
                    double y2 = vehicle.State.Center.Y - vehicle.CameraPosition.Y - distanceInCM2;

                    //if ((hd1 < 30 && y1 > 30 && y1 < 110 && y2 > 30 && y2 < 110) == false)
                    //{
                        w.map.Fixtures.Add(new System.Windows.Media.RectangleGeometry(new Rect(
                            vehicle.State.Center.X + vehicle.CameraPosition.X - (hd2 + hd1) / 2,
                            Math.Min(y1, y2), 1, Math.Abs(y2 - y1))));
                    //}
                    //Line lineShape = new Line();
                    //lineShape.X1 = lineShape.X2 = vehicle.State.Center.X + vehicle.CameraPosition.X - (hd2 + hd1) / 2;
                    //lineShape.Y1 = vehicle.State.Center.Y - vehicle.CameraPosition.Y - distanceInCM1;
                    //lineShape.Y2 = vehicle.State.Center.Y - vehicle.CameraPosition.Y - distanceInCM2;
                    //lineShape.Stroke = Brushes.Red;

                    //canvas.Children.Add(lineShape);
                }
                else
                {
                    img.Draw(line, new Bgr(0, 255, 0), 1);
                    g.DrawString("k=" + k.ToString("f2"), font, System.Drawing.Brushes.Red, (new PointF((line.P1.X + line.P2.X) / 2, (line.P1.Y + line.P2.Y) / 2)));

                }
            }
            g.Dispose();

            CvInvoke.cvShowImage("Line", img);

            //if (collectedBackgroundCount == 1)
            //{
            //w.map.Fixtures.Add(new RectangleGeometry(new Rect(0, 176, 18, 1)));
            //w.map.Fixtures.Add(new RectangleGeometry(new Rect(18, 150, 1, 26)));

            //w.map.Fixtures.Add(new RectangleGeometry(new Rect(0, 150, 18, 0.5)));
            //}
            //else if (collectedBackgroundCount == 2)
            //{
            //    w.map.Fixtures.Add(new RectangleGeometry(new Rect(0, 120, 16.5, 1)));
            //    w.map.Fixtures.Add(new RectangleGeometry(new Rect(16.5, 96.4, 1, 23.6)));
            //}
        }

        private static MapWindow GetMapWindow()
        {
            foreach (var window in App.Current.Windows)
            {
                MapWindow w = window as MapWindow;
                if (w != null)
                    return w;
            }

            MapWindow w1 = new MapWindow();
            w1.Show();
            return w1;
        }


        void CameraVehicle_MotorSpeedChanged(object sender, MotorSpeedChangedEventArgs e)
        {
            backgroundImage = null;

            Dispatcher.Invoke(() =>
            {
                currentTimeTextBlock.Text = e.Tick.ToString();
                leftWheelSpeedTextBlock.Text = e.LeftSpeed.ToString("f2");
                rightWheelSpeedTextBlock.Text = e.RightSpeed.ToString("f2");
            });

            if (saveToFileCheckBox.IsChecked.GetValueOrDefault(false) && saveToFileCheckBox.IsEnabled)
            {
                actionList.Add(e);
            }

            if (e.LeftSpeed == 0 && e.RightSpeed == 0 && lastMotorEvent != null)
            {
                if (lastMotorEvent.LeftSpeed == lastMotorEvent.RightSpeed)
                {
                    double distance = (e.Tick - lastMotorEvent.Tick) / 1000 * lastMotorEvent.LeftSpeed;
                    //vehicle.Forward(distance);

                    MapWindow w = GetMapWindow();
                    w.startingVehicleState = vehicle.State;
                    w.Draw();
                }


            }

            lastMotorEvent = e;


        }


        void timer_Tick(object sender, EventArgs e)
        {

            imageIntervalTextBlock.Text = "距离上次收到图片已有" + ((Environment.TickCount - lastImageTick) / 1000.0) + "秒";

            //if (vehicle.FrontIRSensor != null)
            //    frontSpaceTextBlock.Text = "IR Distance: " + vehicle.FrontIRSensor.Read();

            //if (Environment.TickCount - lastSteeringTick > 2000)
            //    vehicle.SteeringMotor.Off();

        }


        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CameraVehicle vehicle = this.vehicle as CameraVehicle;
                vehicle.ConnectToBrick(connectionComboBox.SelectedValue.ToString());
                buttonsGrid.IsEnabled = true;

                //RecordedActions(vehicle);
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += "\n" + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                        message += "\n......";
                }
                MessageBox.Show(message);
                buttonsGrid.IsEnabled = false;
            }
        }

        private static async void RecordedActions(CameraVehicle vehicle)
        {
            await vehicle.Forward(96);
            await vehicle.ForwardRight(40);
            await vehicle.BackwardLeft(30);
            await vehicle.Backward(15);
            await vehicle.BackwardLeft(20);
            await vehicle.Backward(25);
            await vehicle.BackwardRight(40);
            await vehicle.ForwardLeft(40);
            await vehicle.Backward(4);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                if (actionList != null && actionList.Count > 0)
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<MotorSpeedChangedEventArgs>));

                    using (FileStream stream = File.Create(@"images\actions.xml"))
                    {
                        serializer.Serialize(stream, actionList);
                    }
                    MessageBox.Show("Action list saved.");
                }
                vehicle.Dispose();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void TurnLeftCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            CameraVehicle vehicle = this.vehicle as CameraVehicle;
            e.CanExecute = vehicle != null && vehicle.SteeringAngle > -vehicle.State.TurnMaxDegree;
        }

        private void TurnRightCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            CameraVehicle vehicle = this.vehicle as CameraVehicle;
            e.CanExecute = vehicle != null && vehicle.SteeringAngle < vehicle.State.TurnMaxDegree;
        }

        private void TurnLeftCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //vehicle.SteeringMotor.On(20, 10, true);
            CameraVehicle vehicle = this.vehicle as CameraVehicle;
            //lastSteeringTick = Environment.TickCount;

            vehicle.Steer(vehicle.SteeringAngle - 10);

            SteeringAngle = vehicle.SteeringAngle;

            //UpdateInfoTextBlock();
        }

        private static int RotationToSteering(int rotationDegree)
        {
            return (int)(Math.Asin(0.5 * Math.Sin(rotationDegree * Math.PI / 180)) * 180 / Math.PI);
        }


        private void TurnRightCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //lastSteeringTick = Environment.TickCount;

            CameraVehicle vehicle = this.vehicle as CameraVehicle;
            vehicle.Steer(vehicle.SteeringAngle + 10);

            SteeringAngle = vehicle.SteeringAngle;
            //UpdateInfoTextBlock();
        }

        private void SpeedDownCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CameraVehicle vehicle = this.vehicle as CameraVehicle;
            vehicle.SetSpeed(vehicle.MidSpeed - 4.05);
            //UpdateInfoTextBlock();
        }

        private void SpeedUpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CameraVehicle vehicle = this.vehicle as CameraVehicle;
            vehicle.SetSpeed(vehicle.MidSpeed + 4.05);

            //UpdateInfoTextBlock();
        }

        private void BrakeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CameraVehicle vehicle = this.vehicle as CameraVehicle;
            vehicle.SetSpeed(0);

        }


        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            //SteeringAngle = 0;
            //vehicle.RotationDegree = 0;

        }

        private void listenButton_Click(object sender, RoutedEventArgs e)
        {
            if (interfaceComboBox.SelectedIndex == 0)
            {
                var dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog();
                if (result == CommonFileDialogResult.Cancel)
                    interfaceComboBox.SelectedIndex = -1;
                else
                {
                    vehicle = new SavedCameraVehicle(dialog.FileName);
                    vehicle.ImageAvailable += CameraVehicle_ImageAvailable;
                    vehicle.MotorSpeedChanged += CameraVehicle_MotorSpeedChanged;

                    nextEventButton.Visibility = Visibility.Visible;
                    nextEventButton.IsEnabled = ((SavedCameraVehicle)vehicle).HasNextEvent();

                    ((Button)sender).IsEnabled = false;

                    //saveToFileCheckBox.IsChecked = false;
                    saveToFileCheckBox.IsEnabled = false;

                    eventListBox.ItemsSource = (SavedCameraVehicle)vehicle;
                }
            }
            else
            {
                try
                {
                    ((CameraVehicle)vehicle).ListenToPhone(interfaceComboBox.SelectedItem.ToString());
                    vehicle.ImageAvailable += CameraVehicle_ImageAvailable;
                    ((Button)sender).IsEnabled = false;
                }
                catch (AddressAccessDeniedException ex)
                {
                    MessageBox.Show(ex.Message, "Need Administrator Privilege", MessageBoxButton.OK,
                        MessageBoxImage.Stop);
                    this.Close();
                }
            }
        }

        void CameraVehicle_ImageAvailable(object sender, ImageAvailableEventArgs e)
        {
            //long tick = DateTime.UtcNow.GetUnixTimestamp();
            //Debug.WriteLine("vehicle_ImageAvailable，本机时间{0}，远程时间{1}，相差{2}毫秒。", tick, e.Tick, tick - e.Tick);
            //if (Math.Abs(tick - e.Tick) > 0)
            //{
            //    MessageBox.Show("The computer's time and the phone's time differ too much.");
            //    App.Current.Shutdown(1);
            //}

            lastImageTick = Environment.TickCount;

            currentTimeTextBlock.Text = e.Tick.ToString();
            Image<Bgr, byte> img = e.Image;


            if (showGridCheckBox.IsChecked.GetValueOrDefault(false))
                imageControl.Source = DrawGrid(img).ToBitmapSource();
            else
                imageControl.Source = img.ToBitmapSource();

            if (analysisWorker.IsBusy == false)
            {
                if (getBackgroundCheckBox.IsChecked.GetValueOrDefault(false))
                {
                    if (saveToFileCheckBox.IsEnabled)
                        Task.Factory.StartNew(obj =>
                        {
                            ImageAvailableEventArgs eArgs = (ImageAvailableEventArgs)obj;
                            eArgs.Image.Save(Path.Combine("images", eArgs.Tick + ".jpg"));
                        }, e);

                    analysisWorker.RunWorkerAsync(e);
                }
                else if (saveToFileCheckBox.IsChecked.GetValueOrDefault(false) && saveToFileCheckBox.IsEnabled)
                {
                    Task.Factory.StartNew(obj =>
                    {
                        ImageAvailableEventArgs eArgs = (ImageAvailableEventArgs)obj;
                        eArgs.Image.Save(Path.Combine("images", eArgs.Tick + ".jpg"));
                    }, e);
                }
            }

        }

        private void AnalyzeFrame(object sender, DoWorkEventArgs e)
        {
            Image<Bgr, byte> img = ((ImageAvailableEventArgs)e.Argument).Image;

            if (backgroundImage == null)
                backgroundImage = new Image<Gray, byte>(img.Size);

            //long t = Environment.TickCount;
            Image<Gray, byte> b = GetBackground(img);
            Dispatcher.BeginInvoke(new Action(() => CvInvoke.cvShowImage("Background", img.Copy(b))));

            //Debug.WriteLine("GetBackground: " + (Environment.TickCount - t));
            //t = Environment.TickCount;
            //CvInvoke.cvShowImage("Masked", img);

            Contour<PointInt> contours = b.FindContours();
            Contour<PointInt> biggestContour = contours;
            while (contours != null)
            {
                if (contours.Area > biggestContour.Area)
                    biggestContour = contours;
                contours = contours.HNext;
            }
            //Debug.WriteLine("contours: " + (Environment.TickCount - t));
            //t = Environment.TickCount;

            bool voteFull = false;

            Image<Gray, byte> tempImage = new Image<Gray, byte>(backgroundImage.Size);
            tempImage.Draw(biggestContour, new Gray(10), -1);
            backgroundImage = backgroundImage.Add(tempImage);

            PointInt pUseless = PointInt.Empty;
            voteFull = !backgroundImage.CheckRange(0, 250, ref pUseless);

            if (voteFull)
            {
                collectedBackgroundCount++;
                Dispatcher.Invoke(() =>
                {
                    backgroundImage = backgroundImage - new Gray(130);

                    //w.carRectangle.Width = this.vehicle.State.Width;
                    //w.carRectangle.Height = this.vehicle.State.Length;
                    //w.carRectangle.SetValue(Canvas.LeftProperty, this.vehicle.State.Center.X - this.vehicle.State.Width / 2);
                    //w.carRectangle.SetValue(Canvas.TopProperty, this.vehicle.State.Center.Y + this.vehicle.State.Length / 2);

                    backgroundImage._Dilate(1);
                    //backgroundImage.ROI = new Rectangle(0, backgroundImage.Height - 1, backgroundImage.Width, 1);
                    //backgroundImage._Or(new Image<Gray, byte>(backgroundImage.Width, 1, new Gray(255)));
                    //backgroundImage.ROI = Rectangle.Empty;
                    CvInvoke.cvShowImage("Background-Draw to map",
                        backgroundImage.ThresholdBinary(new Gray(0), new Gray(255)));
                    AddToMap(backgroundImage.ThresholdBinary(new Gray(0), new Gray(255)), img);

                    MapWindow w = GetMapWindow();
                    w.Draw();

                    CameraVehicle vehicle = this.vehicle as CameraVehicle;

                    //if (vehicle != null)
                    //{
                    //    if (collectedBackgroundCount < 3)
                    //        vehicle.SetSpeed(4.05, 5000);
                    //}

                    getBackgroundCheckBox.IsChecked = false;

                    CvInvoke.cvShowImage("VotedBackground", backgroundImage);
                });
            }
            else
                Dispatcher.BeginInvoke(new Action(() => CvInvoke.cvShowImage("VotedBackground", backgroundImage)));
        }



        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="points"></param>
        ///// <param name="img"></param>
        ///// <returns>第一维是图像的通道数量，第二维是数值。第二维的长度等于points.Total。</returns>
        //private static double[][] GetPixelValues(Seq<PointInt> points, Image<Bgr, byte> img)
        //{
        //    double[][] values = new double[3][];

        //    values[0] = new double[points.Total];
        //    values[1] = new double[points.Total];
        //    values[2] = new double[points.Total];
        //    int index = 0;
        //    foreach (PointInt p in points)
        //    {
        //        values[0][index] = img[p].Blue;
        //        values[1][index] = img[p].Green;
        //        values[2][index] = img[p].Red;
        //        index++;
        //    }
        //    return values;
        //}

        //private static void FindColorInRange(Image<Gray, byte> blueImage, double min, double max)
        //{
        //    Image<Gray, byte> result = new Image<Gray, byte>(blueImage.Size);
        //    for (int y = 0; y < blueImage.Height; y++)
        //    {
        //        for (int x = 0; x < blueImage.Width; x++)
        //        {
        //            if (blueImage[y, x].Intensity >= min && blueImage[y, x].Intensity <= max)
        //                result[y, x] = new Gray(255);
        //            else
        //                result[y, x] = new Gray(0);
        //        }
        //    }
        //}

        private Image<Bgr, byte> DrawGrid(Image<Bgr, byte> img)
        {
            img = img.Copy();
            Bgr lineColor = new Bgr(0, 0, 255);
            Font font = new Font("Arial", 16);

            Graphics g = Graphics.FromImage(img.Bitmap);
            for (int d = 40; d < img.Height / 2; d += 20)
            {
                double rd = GetRealVerticalDistaneFromPixelDistance(d);

                string text = rd.ToString("f2");

                g.DrawString(text, font, System.Drawing.Brushes.Red, new PointF(0, img.Height / 2 + d - 8));
                g.DrawString(d + "px", font, System.Drawing.Brushes.Red, new PointF(img.Width - 60, img.Height / 2 + d - 8));

                img.Draw(new LineSegment2D(new PointInt(40, img.Height / 2 + d), new PointInt(img.Width - 60, img.Height / 2 + d)),
                    lineColor, 1);
            }

            for (double realHorizontalDistance = -60; realHorizontalDistance < 60; realHorizontalDistance += 10)
            {
                //图像中点往下40像素。
                double realVerticalDistance1 = GetRealVerticalDistaneFromPixelDistance(40);
                //图像底部往上20像素。
                double realVerticalDistance2 = GetRealVerticalDistaneFromPixelDistance(img.Height / 2 - 20);

                double horizontalPixels1 = realHorizontalDistance * imagingDistance / realVerticalDistance1;
                double horizontalPixels2 = realHorizontalDistance * imagingDistance / realVerticalDistance2;

                img.Draw(new LineSegment2DF(new PointF((float)(img.Width / 2.0 - horizontalPixels1), img.Height / 2 + 40),
                    new PointF((float)(img.Width / 2.0 - horizontalPixels2), img.Height - 20)), lineColor, 1);

                string text = realHorizontalDistance.ToString();

                g.DrawString(text, font, System.Drawing.Brushes.Red, new PointF((float)(img.Width / 2.0 - horizontalPixels1 - 8), img.Height / 2 + 20));
            }

            g.Dispose();
            return img;
        }

        /// <summary>
        /// 获取被测点到摄像机的直线距离，单位厘米。
        /// </summary>
        /// <param name="pixels">被测点到img.Height/2的距离。</param>
        /// <returns></returns>
        double GetRealVerticalDistaneFromPixelDistance(double pixels)
        {
            //if (pixels < 21)
            //    throw new ArgumentOutOfRangeException("pixels",
            //        "The formular used to calculate real distance doesn't work when pixel distance is less than 20.9");
            double w = 1 / pixels;
            return 4203.4 * w + 169297 * w * w;//Mathematica计算得出。
            //return imagingDistance * cameraHeight * 1 / pixels;
        }

        double GetRealHorizontalDistanceFromPixelDistance(double realVerticalDistance, double horizontalPixels)
        {
            return realVerticalDistance / imagingDistance * horizontalPixels;
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //if (this.IsLoaded && (serviceHost == null || serviceHost.State == CommunicationState.Closed))
            //{
            //    Emgu.CV.Image<Bgr, byte> img =
            //        new Emgu.CV.Image<Bgr, byte>(
            //            @"C:\Users\loveright\Documents\Visual Studio 2013\Projects\乐高\电脑端\bin\Debug\images\1.jpg");

            //    img = DrawGrid(img);

            //    imageControl.Source = img.ToBitmapSource();
            //}
        }


        private Image<Gray, byte> GetBackground(Image<Bgr, byte> image)
        {
            //设定地面区域
            int left = image.Width / 4;
            int width = image.Width / 2;
            int top = image.Height / 16 * 13;
            int height = image.Height / 8;

            image.ROI = new Rectangle(left, top, width, height);


            //long t = Environment.TickCount;

            int[] thresholds = GetNeighborDifference(image);

            //Debug.WriteLine("GetNeighborDifference:" + (Environment.TickCount - t));

            image.ROI = Rectangle.Empty;

            ConcurrentQueue<PointInt> connectedPoints = new ConcurrentQueue<PointInt>();
            //选区内是黑色，选区外是白色。
            Gray selectionInColor = new Gray(255);
            Gray selectionOutColor = new Gray(0);
            Image<Gray, byte> backgroundImage = new Image<Gray, byte>(image.Width, image.Height, selectionOutColor);
            //加入
            backgroundImage.ROI = new Rectangle(left, top, width, height);
            backgroundImage._Or(new Image<Gray, byte>(width, height, selectionInColor));
            backgroundImage.ROI = Rectangle.Empty;

            for (int x = left; x < left + width; x++)
            {
                connectedPoints.Enqueue(new PointInt(x, top));
                connectedPoints.Enqueue(new PointInt(x, top + height));
            }

            for (int y = top; y < top + height; y++)
            {
                connectedPoints.Enqueue(new PointInt(left, y));
                connectedPoints.Enqueue(new PointInt(left + width, y));
            }

            //t = Environment.TickCount;
            for (; ; )
            {
                var nextQueue = new ConcurrentQueue<PointInt>();
                int w = image.Width;
                int h = image.Height;
                Parallel.ForEach(connectedPoints, p =>
                {
                    if (backgroundImage[p].Equals(selectionInColor))
                        return;

                    backgroundImage[p] = selectionInColor;

                    Bgr color = image[p];
                    if (p.X > 0 && image[p.Y, p.X - 1].IsSimilar(color, thresholds[0], thresholds[1], thresholds[2]))
                        nextQueue.Enqueue(new PointInt(p.X - 1, p.Y));

                    if (p.X < w - 1 &&
                        image[p.Y, p.X + 1].IsSimilar(color, thresholds[0], thresholds[1], thresholds[2]))
                        nextQueue.Enqueue(new PointInt(p.X + 1, p.Y));

                    if (p.Y > 0 && image[p.Y - 1, p.X].IsSimilar(color, thresholds[0], thresholds[1], thresholds[2]))
                        nextQueue.Enqueue(new PointInt(p.X, p.Y - 1));

                    if (p.Y < h - 1 &&
                        image[p.Y + 1, p.X].IsSimilar(color, thresholds[0], thresholds[1], thresholds[2]))
                        nextQueue.Enqueue(new PointInt(p.X, p.Y + 1));
                });
                if (nextQueue.Count > 0)
                    connectedPoints = nextQueue;
                else
                    break;
            }
            //Debug.WriteLine("connectedPoints:" + (Environment.TickCount - t));

            backgroundImage._Dilate(3);
            backgroundImage._Erode(3);

            return backgroundImage;
        }

        /// <summary>
        /// 获取指定图像（或ROI）中每个像素与其周围像素的差的90百分位数，
        /// 即90%的像素与其周围像素的差都小于这个返回值。
        /// </summary>
        /// <param name="image"></param>
        /// <returns>每个通道的像素的差的90百分位数。</returns>
        private int[] GetNeighborDifference(Image<Bgr, byte> image)
        {
            int[] differences = new int[image.NumberOfChannels];
            for (int ch = 0; ch < image.NumberOfChannels; ch++)
            {
                int[] differenceCount = new int[255];

                Image<Gray, byte> chImage = image[ch];//此操作复制一份通道图像，慢。
                Parallel.For(1, image.Height - 1, y =>
                {
                    for (int x = 1; x < image.Width - 1; x++)
                    {
                        int d = (int)Math.Abs(chImage[y, x].Intensity - chImage[y - 1, x].Intensity);
                        Interlocked.Increment(ref differenceCount[d]);

                        d = (int)Math.Abs(chImage[y, x].Intensity - chImage[y + 1, x].Intensity);
                        Interlocked.Increment(ref differenceCount[d]);

                        d = (int)Math.Abs(chImage[y, x].Intensity - chImage[y, x - 1].Intensity);
                        Interlocked.Increment(ref differenceCount[d]);

                        d = (int)Math.Abs(chImage[y, x].Intensity - chImage[y, x + 1].Intensity);
                        Interlocked.Increment(ref differenceCount[d]);
                    }
                });

                double v = differenceCount.Sum() * 0.9;//90%的数都要小于等于v。
                int sum = 0;
                for (int i = 0; i < differenceCount.Length; i++)
                {
                    int d = differenceCount[i];
                    sum += d;
                    if (sum > v)
                    {
                        differences[ch] = i; //一定会被赋值。
                        break;
                    }
                }
            }
            return differences;
        }

        private void nextEventButton_Click(object sender, RoutedEventArgs e)
        {
            var vehicle = (SavedCameraVehicle)this.vehicle;
            vehicle.RaiseNextEvent();
            nextEventButton.IsEnabled = vehicle.HasNextEvent();

            //if (Convert.ToInt32(autoRaiseCountTextBox.Text) > 1)
            //{
            //    autoRaiseCountTextBox.Text = (Convert.ToInt32(autoRaiseCountTextBox.Text) - 1).ToString();

            //    Dispatcher.BeginInvoke(new Action<object, RoutedEventArgs>(nextEventButton_Click), sender, e);
            //}
        }

        private void saveToFileCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (((CheckBox)sender).IsChecked.GetValueOrDefault(false) && actionList == null)
                actionList = new List<MotorSpeedChangedEventArgs>();
        }

        private void getBackgroundCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            backgroundImage = null;
        }

        private void runButton_Click(object sender, RoutedEventArgs e)
        {
            RecordedActions((CameraVehicle)vehicle);
        }
    }


}
