using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Emgu.CV;
using Emgu.CV.Structure;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;
using System.Diagnostics;

namespace Gqqnbig.Lego
{

    /// <summary>
    /// CameraCalibrationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CameraCalibrationWindow : Window
    {

        #region Display and aquaring chess board info
        Capture _Capture; // capture device
        Image<Bgr, Byte> img; // image captured
        Image<Gray, Byte> Gray_Frame; // image for processing
        const int width = 9;//9 //width of chessboard no. squares in width - 1
        const int height = 6;//6 // heght of chess board no. squares in heigth - 1
        Size patternSize = new Size(width, height); //size of chess board to be detected
        Bgr[] line_colour_array = new Bgr[width * height]; // just for displaying coloured lines of detected chessboard

        static Image<Gray, Byte>[] Frame_array_buffer = new Image<Gray, byte>[100]; //number of images to calibrate camera over
        int frame_buffer_savepoint = 0;
        bool start_Flag = false;
        #endregion

        #region Current mode variables
        public enum Mode
        {
            Caluculating_Intrinsics,
            Calibrated,
            SavingFrames
        }
        Mode currentMode = Mode.SavingFrames;
        #endregion

        #region Getting the camera calibration
        MCvPoint3D32f[][] corners_object_list = new MCvPoint3D32f[Frame_array_buffer.Length][];
        PointF[][] corners_points_list = new PointF[Frame_array_buffer.Length][];

        IntrinsicCameraParameters IC = new IntrinsicCameraParameters();
        ExtrinsicCameraParameters[] EX_Param;

        #endregion


        //Queue<Image<Bgr, byte>> images = new Queue<Image<Bgr, byte>>();
        private bool isAnalyzing = false;
        //private ServiceHost serviceHost;

        readonly CameraVehicle vehicle = new CameraVehicle();

        public CameraCalibrationWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                vehicle.ListenToPhone("192.168.173.1");
                PhoneService.ImageReceived += phoneService_ImageReceived;
            }
            catch (AddressAccessDeniedException ex)
            {
                MessageBox.Show(ex.Message, "Need Administrator Privilege", MessageBoxButton.OK, MessageBoxImage.Stop);
                this.Close();
            }
        }

        private void phoneService_ImageReceived(object sender, ImageReceivedEventArgs e)
        {
            Debug.WriteLine("phoneService_ImageReceived");

            if (isAnalyzing == false)
            {
                isAnalyzing = true;
                Debug.WriteLine("开始分析");
                Image<Bgr, byte> img = new Image<Bgr, byte>(e.Width, e.Height);
                for (int y = 0; y < e.Height; y++)
                    for (int x = 0; x < e.Width; x++)
                        for (int ch = 0; ch < 3; ch++)
                            img.Data[y, x, ch] = e.ImageData[y * e.Width * 4 + x * 4 + ch];

                Task analyzeTask = new Task(o => AnalyzeImage((Image<Bgr, byte>)o), img);
                analyzeTask.Start();
            }

        }

        private void AnalyzeImage(Image<Bgr, byte> img)
        {
            Gray_Frame = img.Convert<Gray, Byte>();

            //apply chess board detection
            if (currentMode == Mode.SavingFrames)
            {
                var corners = CameraCalibration.FindChessboardCorners(Gray_Frame, patternSize,
                    Emgu.CV.CvEnum.CALIB_CB_TYPE.ADAPTIVE_THRESH);
                //we use this loop so we can show a colour image rather than a gray: //CameraCalibration.DrawChessboardCorners(Gray_Frame, patternSize, corners);

                Dispatcher.Invoke(() => addButton.IsEnabled = start_Flag && corners != null);
                if (corners != null) //chess board found
                {
                    //make mesurments more accurate by using FindCornerSubPixel
                    Gray_Frame.FindCornerSubPix(new PointF[1][] { corners }, new Size(11, 11), new Size(-1, -1),
                        new MCvTermCriteria(30, 0.1));

                    //dram the results
                    img.Draw(new CircleF(corners[0], 3), new Bgr(Color.Yellow), 1);
                    for (int i = 1; i < corners.Length; i++)
                    {
                        img.Draw(new LineSegment2DF(corners[i - 1], corners[i]), line_colour_array[i], 2);
                        img.Draw(new CircleF(corners[i], 3), new Bgr(Color.Yellow), 1);
                    }
                    //calibrate the delay bassed on size of buffer
                    //if buffer small you want a big delay if big small delay
                    Thread.Sleep(100); //allow the user to move the board to a different position
                }
                corners = null;
            }
            if (currentMode == Mode.Caluculating_Intrinsics)
            {
                //we can do this in the loop above to increase speed
                for (int k = 0; k < Frame_array_buffer.Length; k++)
                {
                    corners_points_list[k] = CameraCalibration.FindChessboardCorners(Frame_array_buffer[k], patternSize,
                        Emgu.CV.CvEnum.CALIB_CB_TYPE.ADAPTIVE_THRESH);
                    //for accuracy
                    Gray_Frame.FindCornerSubPix(corners_points_list, new Size(11, 11), new Size(-1, -1),
                        new MCvTermCriteria(30, 0.1));

                    //Fill our objects list with the real world mesurments for the intrinsic calculations
                    List<MCvPoint3D32f> object_list = new List<MCvPoint3D32f>();
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            object_list.Add(new MCvPoint3D32f(j * 20.0F, i * 20.0F, 0.0F));
                        }
                    }
                    corners_object_list[k] = object_list.ToArray();
                }

                //our error should be as close to 0 as possible

                double error = CameraCalibration.CalibrateCamera(corners_object_list, corners_points_list, Gray_Frame.Size, IC,
                    Emgu.CV.CvEnum.CALIB_TYPE.CV_CALIB_RATIONAL_MODEL, new MCvTermCriteria(10), out EX_Param);
                //If Emgu.CV.CvEnum.CALIB_TYPE == CV_CALIB_USE_INTRINSIC_GUESS and/or CV_CALIB_FIX_ASPECT_RATIO are specified, some or all of fx, fy, cx, cy must be initialized before calling the function
                //if you use FIX_ASPECT_RATIO and FIX_FOCAL_LEGNTH options, these values needs to be set in the intrinsic parameters before the CalibrateCamera function is called. Otherwise 0 values are used as default.
                MessageBox.Show("Intrinsci Calculation Error: " + error.ToString(), "Results");
                //display the results to the user
                currentMode = Mode.Calibrated;
            }
            if (currentMode == Mode.Calibrated)
            {
                //display the original image
                Dispatcher.Invoke(() => Sub_PicturBox.Source = img.ToBitmapSource());
                //calculate the camera intrinsics
                Matrix<float> Map1, Map2;
                IC.InitUndistortMap(img.Width, img.Height, out Map1, out Map2);

                //remap the image to the particular intrinsics
                //In the current version of EMGU any pixel that is not corrected is set to transparent allowing the original image to be displayed if the same
                //image is mapped backed, in the future this should be controllable through the flag '0'
                Image<Bgr, Byte> temp = img.CopyBlank();
                CvInvoke.cvRemap(img, temp, Map1, Map2, 0, new MCvScalar(0));
                img = temp.Copy();

                //set up to allow another calculation
                //SetButtonState(true);
                start_Flag = false;
            }
            Dispatcher.Invoke(() => Main_Picturebox.Source = img.ToBitmapSource());
            isAnalyzing = false;
            Debug.WriteLine("分析完毕");
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode != Mode.SavingFrames)
                currentMode = Mode.SavingFrames;
            Start_BTN.IsEnabled = false;
            //set up the arrays needed
            Frame_array_buffer = new Image<Gray, byte>[1];
            corners_object_list = new MCvPoint3D32f[Frame_array_buffer.Length][];
            corners_points_list = new PointF[Frame_array_buffer.Length][];
            frame_buffer_savepoint = 0;
            //allow the start
            start_Flag = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            vehicle.Dispose();
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            //if go button has been pressed start aquiring frames else we will just display the points
            if (start_Flag)
            {
                Frame_array_buffer[frame_buffer_savepoint] = Gray_Frame.Copy(); //store the image
                frame_buffer_savepoint++; //increase buffer positon

                //check the state of buffer
                if (frame_buffer_savepoint == Frame_array_buffer.Length)
                    currentMode = Mode.Caluculating_Intrinsics; //buffer full
            }
        }
    }
}
