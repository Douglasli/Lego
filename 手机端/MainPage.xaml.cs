using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Phone.Media.Capture;
using Gqqnbig;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Shell;
using sdkBasicCameraCS.ServiceReference1;
using Environment = System.Environment;
using Rect = Windows.Foundation.Rect;
using Size = Windows.Foundation.Size;

namespace sdkBasicCameraCS
{
    public partial class MainPage : PhoneApplicationPage
    {
        /// <summary>
        /// 发送图像的时间间隔，单位毫秒。
        /// </summary>
        private int sendImageInterval = 100;
        private int lastSendImageTick;
        int imageSeqeuenceNumber;
        //PhotoCamera cam;
        //MediaLibrary library = new MediaLibrary();

        // Holds the current flash mode.
        //private string currentFlashMode;


        //Socket streamScoket;
        private IPhoneService phoneService;
        private ChannelFactory<IPhoneService> factory;
        //private DispatcherTimer timer;
        private PhotoCaptureDevice cam;
        private Size resolution;

        public MainPage()
        {
            InitializeComponent();

        }

        //Code for initialization, capture completed, image availability events; also setting the source for the viewfinder.
        protected async override void OnNavigatedTo(NavigationEventArgs e) //当页面变为框架中的活动页面时调用。
        {

            var resolutions = PhotoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Back);
            resolution = resolutions.Aggregate((x, y) => x.Height * x.Width < y.Height * y.Width ? x : y);

            resolutionsListBox.ItemsSource = resolutions;

            EndpointAddress address;
            if (App.LocalAddress.StartsWith("192.168"))
                address = new EndpointAddress("http://192.168.173.1:8000/phone");
            else
                address = new EndpointAddress("http://desk.gqqnbig.me:8000/phone");
            factory = new ChannelFactory<IPhoneService>(new BasicHttpBinding(), address);
            //factory.Opened += factory_Opened;
            phoneService = factory.CreateChannel(address);
            imageSeqeuenceNumber = 0;

            if (PhotoCaptureDevice.AvailableSensorLocations.Contains(CameraSensorLocation.Back))
            {
                cam = await PhotoCaptureDevice.OpenAsync(CameraSensorLocation.Back, resolution);
                phoneService.BeginStartSession(DateTime.UtcNow.GetUnixTimestamp() - App.TimeDifference, (int)resolution.Width, (int)resolution.Height,
                    StartSessionCompleted, 0);
                cam.SetProperty(KnownCameraPhotoProperties.FlashMode, FlashState.Off);
                //cam.SetProperty(KnownCameraPhotoProperties.SceneMode, CameraSceneMode.Landscape);
                cam.SetProperty(KnownCameraGeneralProperties.AutoFocusRange, AutoFocusRange.Infinity);
                cam.SetProperty(KnownCameraPhotoProperties.LockedAutoFocusParameters, AutoFocusParameters.Focus);

                await cam.SetPreviewResolutionAsync(resolution);

                viewfinderBrush.SetSource(cam);

                //CameraButtons.ShutterKeyPressed += ShutterButton_Click;

            }
            else
            {
                MessageBox.Show("Camera is not available. Exiting...");
                Application.Current.Terminate();
                return;
            }

            //var videoCaptureDevice = await AudioVideoCaptureDevice.OpenAsync(CameraSensorLocation.Back, AudioVideoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Back).First());

            //videoCaptureDevice.SetProperty(KnownCameraAudioVideoProperties.VideoTorchMode, VideoTorchMode.On);
            //videoCaptureDevice.sta



            PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;



            //timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromSeconds(0.2);
            //timer.Tick += (sender, e1) =>
            //{
            //    ShutterButton_Click(null, null);
            //};
            //timer.Start();

        }

        private void StartSessionCompleted(IAsyncResult a)
        {
            try
            {
                if (a.AsyncState.Equals(1))
                {
                    phoneService.EndStartSession(a);
                    cam.PreviewFrameAvailable += cam_PreviewFrameAvailable;
                }
                else
                {
                    phoneService.BeginStartSession(DateTime.UtcNow.GetUnixTimestamp() - App.TimeDifference,
                        (int)resolution.Width, (int)resolution.Height, StartSessionCompleted, 1);
                }
            }
            catch (Exception e)
            {
                Dispatcher.BeginInvoke(new Func<Exception, string>(exc => txtDebug.Text = exc.Message), e);
            }
        }

        //void factory_Opened(object sender, EventArgs e)
        //{

        //imageSeqeuenceNumber = 0;
        //phoneService.BeginStartSession(0, (int)resolution.Width, (int)resolution.Height,
        //    a =>
        //    {
        //        phoneService.EndStartSession(a);
        //        cam.PreviewFrameAvailable += cam_PreviewFrameAvailable;
        //    }, null);
        //}

        void cam_PreviewFrameAvailable(ICameraCaptureDevice sender, object args)
        {
            if (Environment.TickCount - lastSendImageTick < sendImageInterval)
                return;

            int[] pixels = new int[(int)Math.Ceiling(resolution.Width * resolution.Height)];
            sender.GetPreviewBufferArgb(pixels);

            List<byte> buffer = new List<byte>(pixels.Length * 4);
            foreach (int v in pixels)
                buffer.AddRange(BitConverter.GetBytes(v));

            if (phoneService != null)
                phoneService.BeginSendImage(DateTime.UtcNow.GetUnixTimestamp() - App.TimeDifference, buffer.ToArray(), SendImageCompleted, null);
            sendImageInterval += 100;
            //Dispatcher.BeginInvoke(() => { txtDebug.Text = "sendImageInterval=" + sendImageInterval; });
        }


        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) //当页面不再是框架中的活动页面时调用。
        {
            if (cam != null)
            {
                cam.Dispose();
                cam = null;
            }


            if (cam != null)
            {
                // Dispose camera to minimize power consumption and to expedite shutdown.
                cam.Dispose();

                // Release memory, ensure garbage collection.
                cam.PreviewFrameAvailable -= cam_PreviewFrameAvailable;
                //cam.Initialized -= cam_Initialized;
                //cam.CaptureCompleted -= cam_CaptureCompleted;
                //cam.CaptureImageAvailable -= cam_CaptureImageAvailable;
                //cam.CaptureThumbnailAvailable -= cam_CaptureThumbnailAvailable;
                //cam.AutoFocusCompleted -= cam_AutoFocusCompleted;
                //CameraButtons.ShutterKeyHalfPressed -= OnButtonHalfPress;
                //CameraButtons.ShutterKeyPressed -= OnButtonFullPress;
                //CameraButtons.ShutterKeyReleased -= OnButtonRelease;

                cam = null;
            }

            if (factory != null)
            {
                factory.Close();
                factory = null;
            }

            //timer.Stop();

            //PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Enabled;
        }

        // Update the UI if initialization succeeds.
        //void cam_Initialized(object sender, CameraOperationCompletedEventArgs e)
        //{
        //if (e.Succeeded)
        //{
        //    cam.FlashMode = FlashMode.Off;

        //    IEnumerable<Size> resList = cam.AvailableResolutions;
        //    Size res = resList.Aggregate((x, y) => x.Height * x.Width < y.Height * y.Width ? x : y);
        //    cam.Resolution = res;

        //    this.Dispatcher.BeginInvoke(delegate()
        //    {
        //        timer.Start();
        //        // Write message.
        //        txtDebug.Text = "Camera initialized.";

        //        // Set flash button text.
        //        //FlashButton.Content = "Fl:" + cam.FlashMode.ToString();
        //    });
        //}
        //}

        // Ensure that the viewfinder is upright in LandscapeRight.
        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            //if (cam != null)
            //{
            //    // LandscapeRight rotation when camera is on back of device.
            //    int landscapeRightRotation = 180;

            //    // Change LandscapeRight rotation for front-facing camera.
            //    if (cam.CameraType == CameraType.FrontFacing) landscapeRightRotation = -180;

            //    // Rotate video brush from camera.
            //    if (e.Orientation == PageOrientation.LandscapeRight)
            //    {
            //        // Rotate for LandscapeRight orientation.
            //        viewfinderBrush.RelativeTransform =
            //            new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = landscapeRightRotation };
            //    }
            //    else
            //    {
            //        // Rotate for standard landscape orientation.
            //        viewfinderBrush.RelativeTransform =
            //            new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = 0 };
            //    }
            //}

            base.OnOrientationChanged(e);
        }

        //private async void ShutterButton_Click(object sender, EventArgs eventArgs)
        //{

        //    if (cam != null)
        //    {
        //        CameraCaptureSequence captureSequence = cam.CreateCaptureSequence(1);
        //        MemoryStream captureStream = new MemoryStream();

        //        captureSequence.Frames[0].CaptureStream = captureStream.AsOutputStream();

        //        await cam.PrepareCaptureSequenceAsync(captureSequence);
        //        await captureSequence.StartCaptureAsync();

        //        captureStream.Seek(0, SeekOrigin.Begin);
        //        byte[] buffer = new byte[captureStream.Length];
        //        captureStream.Read(buffer, 0, buffer.Length);

        //        phoneService.BeginSendImage(++imageSeqeuenceNumber, buffer, SendImageCompleted, null);

        //        //timer.Start();
        //        //    Dispatcher.BeginInvoke(timer.Stop);
        //        //    try
        //        //    {
        //        //        //Debug.Assert(cam.FlashMode == FlashMode.Off);
        //        //        cam.FlashMode = FlashMode.Off;
        //        //        cam.CaptureImage();
        //        //    }
        //        //    catch (Exception ex)
        //        //    {
        //        //        this.Dispatcher.BeginInvoke(delegate()
        //        //        {
        //        //            // Cannot capture an image until the previous capture has completed.
        //        //            txtDebug.Text = ex.Message;
        //        //        });
        //        //    }
        //    }
        //}

        //void cam_CaptureCompleted(object sender, CameraOperationCompletedEventArgs e)
        //{
        //    // Increments the savedCounter variable used for generating JPEG file names.
        //    savedCounter++;
        //}


        // Informs when full resolution picture has been taken, saves to local media library and isolated storage.
        //void cam_CaptureImageAvailable(object sender, ContentReadyEventArgs e)
        //{
        //    //string fileName = savedCounter + ".jpg";

        //    if (phoneService != null)
        //    {
        //        //SocketAsyncEventArgs se = new SocketAsyncEventArgs();
        //        //se.Buffer = new byte[];
        //        e.ImageStream.Seek(0, SeekOrigin.Begin);
        //        byte[] buffer = new byte[e.ImageStream.Length];
        //        e.ImageStream.Read(buffer, 0, buffer.Length);
        //        //e.ImageStream.ReadAsync(buffer, 0, buffer.Length);

        //        //se.SetBuffer(buffer, 0, buffer.Length);

        //        //streamScoket.SendAsync(se);

        //        e.ImageStream.Close();


        //        IAsyncResult r = phoneService.BeginSendImage(++imageSeqeuenceNumber, buffer, SendImageCompleted, null);
        //        //phoneService.EndSendImage(r);
        //        //Dispatcher.BeginInvoke(timer.Start);

        //    }
        //    //try
        //    //{   // Write message to the UI thread.
        //    //    Deployment.Current.Dispatcher.BeginInvoke(delegate()
        //    //    {
        //    //        txtDebug.Text = "Captured image available, saving picture.";
        //    //    });

        //    //    // Save picture to the library camera roll.
        //    //    library.SavePictureToCameraRoll(fileName, e.ImageStream);

        //    //    // Write message to the UI thread.
        //    //    Deployment.Current.Dispatcher.BeginInvoke(delegate()
        //    //    {
        //    //        txtDebug.Text = "Picture has been saved to camera roll.";

        //    //    });

        //    //    // Set the position of the stream back to start
        //    //    e.ImageStream.Seek(0, SeekOrigin.Begin);

        //    //    // Save picture as JPEG to isolated storage.
        //    //    using (IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication())
        //    //    {
        //    //        using (IsolatedStorageFileStream targetStream = isStore.OpenFile(fileName, FileMode.Create, FileAccess.Write))
        //    //        {
        //    //            // Initialize the buffer for 4KB disk pages.
        //    //            byte[] readBuffer = new byte[4096];
        //    //            int bytesRead = -1;

        //    //            // Copy the image to isolated storage. 
        //    //            while ((bytesRead = e.ImageStream.Read(readBuffer, 0, readBuffer.Length)) > 0)
        //    //            {
        //    //                targetStream.Write(readBuffer, 0, bytesRead);
        //    //            }
        //    //        }
        //    //    }

        //    //    // Write message to the UI thread.
        //    //    Deployment.Current.Dispatcher.BeginInvoke(delegate()
        //    //    {
        //    //        txtDebug.Text = "Picture has been saved to isolated storage.";

        //    //    });
        //    //}
        //    //finally
        //    //{
        //    //    // Close image stream
        //    //    e.ImageStream.Close();
        //    //}

        //}

        private void SendImageCompleted(IAsyncResult ar)
        {
            try
            {
                phoneService.EndSendImage(ar);

            }
            catch (Exception e)
            {
                Delegate d = new Func<Exception, string>(exc => txtDebug.Text = exc.Message);
                this.Dispatcher.BeginInvoke(d, e);
            }
            finally
            {
                lastSendImageTick = Environment.TickCount;
                sendImageInterval -= 100;
                Dispatcher.BeginInvoke(() => { txtDebug.Text = "sendImageInterval=" + sendImageInterval; });
            }
        }

        //// Informs when thumbnail picture has been taken, saves to isolated storage
        //// User will select this image in the pictures application to bring up the full-resolution picture. 
        //public void cam_CaptureThumbnailAvailable(object sender, ContentReadyEventArgs e)
        //{
        //    string fileName = savedCounter + "_th.jpg";

        //    try
        //    {
        //        // Write message to UI thread.
        //        Deployment.Current.Dispatcher.BeginInvoke(delegate()
        //        {
        //            txtDebug.Text = "Captured image available, saving thumbnail.";
        //        });

        //        // Save thumbnail as JPEG to isolated storage.
        //        using (IsolatedStorageFile isStore = IsolatedStorageFile.GetUserStoreForApplication())
        //        {
        //            using (IsolatedStorageFileStream targetStream = isStore.OpenFile(fileName, FileMode.Create, FileAccess.Write))
        //            {
        //                // Initialize the buffer for 4KB disk pages.
        //                byte[] readBuffer = new byte[4096];
        //                int bytesRead = -1;

        //                // Copy the thumbnail to isolated storage. 
        //                while ((bytesRead = e.ImageStream.Read(readBuffer, 0, readBuffer.Length)) > 0)
        //                {
        //                    targetStream.Write(readBuffer, 0, bytesRead);
        //                }
        //            }
        //        }

        //        // Write message to UI thread.
        //        Deployment.Current.Dispatcher.BeginInvoke(delegate()
        //        {
        //            txtDebug.Text = "Thumbnail has been saved to isolated storage.";

        //        });
        //    }
        //    finally
        //    {
        //        // Close image stream
        //        e.ImageStream.Close();
        //    }
        //}

        // Activate a flash mode.
        // Cycle through flash mode options when the flash button is pressed.
        //private void changeFlash_Clicked(object sender, RoutedEventArgs e)
        //{

        //    switch (cam.FlashMode)
        //    {
        //        case FlashMode.Off:
        //            if (cam.IsFlashModeSupported(FlashMode.On))
        //            {
        //                // Specify that flash should be used.
        //                cam.FlashMode = FlashMode.On;
        //                FlashButton.Content = "Fl:On";
        //                currentFlashMode = "Flash mode: On";
        //            }
        //            break;
        //        case FlashMode.On:
        //            if (cam.IsFlashModeSupported(FlashMode.RedEyeReduction))
        //            {
        //                // Specify that the red-eye reduction flash should be used.
        //                cam.FlashMode = FlashMode.RedEyeReduction;
        //                FlashButton.Content = "Fl:RER";
        //                currentFlashMode = "Flash mode: RedEyeReduction";
        //            }
        //            else if (cam.IsFlashModeSupported(FlashMode.Auto))
        //            {
        //                // If red-eye reduction is not supported, specify automatic mode.
        //                cam.FlashMode = FlashMode.Auto;
        //                FlashButton.Content = "Fl:Auto";
        //                currentFlashMode = "Flash mode: Auto";
        //            }
        //            else
        //            {
        //                // If automatic is not supported, specify that no flash should be used.
        //                cam.FlashMode = FlashMode.Off;
        //                FlashButton.Content = "Fl:Off";
        //                currentFlashMode = "Flash mode: Off";
        //            }
        //            break;
        //        case FlashMode.RedEyeReduction:
        //            if (cam.IsFlashModeSupported(FlashMode.Auto))
        //            {
        //                // Specify that the flash should be used in the automatic mode.
        //                cam.FlashMode = FlashMode.Auto;
        //                FlashButton.Content = "Fl:Auto";
        //                currentFlashMode = "Flash mode: Auto";
        //            }
        //            else
        //            {
        //                // If automatic is not supported, specify that no flash should be used.
        //                cam.FlashMode = FlashMode.Off;
        //                FlashButton.Content = "Fl:Off";
        //                currentFlashMode = "Flash mode: Off";
        //            }
        //            break;
        //        case FlashMode.Auto:
        //            if (cam.IsFlashModeSupported(FlashMode.Off))
        //            {
        //                // Specify that no flash should be used.
        //                cam.FlashMode = FlashMode.Off;
        //                FlashButton.Content = "Fl:Off";
        //                currentFlashMode = "Flash mode: Off";
        //            }
        //            break;
        //    }

        //    // Display current flash mode.
        //    this.Dispatcher.BeginInvoke(delegate()
        //    {
        //        txtDebug.Text = currentFlashMode;
        //    });
        //}

        // Provide auto-focus in the viewfinder.
        private void focus_Clicked(object sender, RoutedEventArgs e)
        {
            //if (cam.IsFocusSupported == true)
            //{
            //    //Focus when a capture is not in progress.
            //    try
            //    {
            //        cam.Focus();
            //    }
            //    catch (Exception focusError)
            //    {
            //        // Cannot focus when a capture is in progress.
            //        this.Dispatcher.BeginInvoke(delegate()
            //        {
            //            txtDebug.Text = focusError.Message;
            //        });
            //    }
            //}
            //else
            //{
            //    // Write message to UI.
            //    this.Dispatcher.BeginInvoke(delegate()
            //    {
            //        txtDebug.Text = "Camera does not support programmable auto focus.";
            //    });
            //}
        }

        void cam_AutoFocusCompleted(object sender, CameraOperationCompletedEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(delegate()
            {
                // Write message to UI.
                txtDebug.Text = "Auto focus has completed.";

                // Hide the focus brackets.
                focusBrackets.Visibility = Visibility.Collapsed;

            });
        }

        // Provide touch focus in the viewfinder.
        async void focus_Tapped(object sender, GestureEventArgs e)
        {
            if (cam == null) return;


            Point uiTapPoint = e.GetPosition(viewfinderCanvas);

            if (PhotoCaptureDevice.IsFocusRegionSupported(cam.SensorLocation))
            {
                Size _focusRegionSize = new Size(100, 100);

                // Get tap coordinates as a foundation point
                Windows.Foundation.Point tapPoint = new Windows.Foundation.Point(uiTapPoint.X, uiTapPoint.Y);

                double xRatio = viewfinderCanvas.ActualHeight / cam.PreviewResolution.Width;
                double yRatio = viewfinderCanvas.ActualWidth / cam.PreviewResolution.Height;

                // adjust to center focus on the tap point
                Windows.Foundation.Point displayOrigin = new Windows.Foundation.Point(
                            tapPoint.Y - _focusRegionSize.Width / 2, (viewfinderCanvas.ActualWidth - tapPoint.X) - _focusRegionSize.Height / 2);

                // adjust for resolution difference between preview image and the canvas
                Windows.Foundation.Point viewFinderOrigin = new Windows.Foundation.Point(displayOrigin.X / xRatio, displayOrigin.Y / yRatio);

                Rect focusrect = new Rect(viewFinderOrigin, _focusRegionSize);

                // clip to preview resolution
                Rect viewPortRect = new Rect(0, 0, cam.PreviewResolution.Width, cam.PreviewResolution.Height);
                focusrect.Intersect(viewPortRect);

                cam.FocusRegion = focusrect;

                // show a focus indicator
                //focusBrackets.SetValue(Shape.StrokeProperty, new SolidColorBrush(Colors.Blue));
                focusBrackets.Visibility = Visibility.Visible;
                focusBrackets.SetValue(Canvas.LeftProperty, uiTapPoint.X - _focusRegionSize.Width / 2);
                focusBrackets.SetValue(Canvas.TopProperty, uiTapPoint.Y - _focusRegionSize.Height / 2);

                CameraFocusStatus status = await cam.FocusAsync();

                if (status == CameraFocusStatus.Locked)
                {
                    //focusBrackets.SetValue(Shape.StrokeProperty, new SolidColorBrush(Colors.Green));
                    cam.SetProperty(KnownCameraPhotoProperties.LockedAutoFocusParameters, AutoFocusParameters.Focus);
                }
                else
                {
                    cam.SetProperty(KnownCameraPhotoProperties.LockedAutoFocusParameters, AutoFocusParameters.None);
                }

            }
        }

        private void changeRes_Clicked(object sender, RoutedEventArgs e)
        {
            //IEnumerable<Size> resList = cam.AvailableResolutions;
            //Size res = resList.Aggregate((x, y) => x.Height * x.Width < y.Height * y.Width ? x : y);
            //cam.Resolution = res;

            ////// Update the UI.
            //txtDebug.Text = String.Format("Setting capture resolution: {0}x{1}", res.Width, res.Height);
            ////ResButton.Content = "R" + res.Width;
        }


        // Provide auto-focus with a half button press using the hardware shutter button.
        private void OnButtonHalfPress(object sender, EventArgs e)
        {
            //if (cam != null)
            //{
            //    // Focus when a capture is not in progress.
            //    try
            //    {
            //        this.Dispatcher.BeginInvoke(delegate()
            //        {
            //            txtDebug.Text = "Half Button Press: Auto Focus";
            //        });

            //        cam.Focus();
            //    }
            //    catch (Exception focusError)
            //    {
            //        // Cannot focus when a capture is in progress.
            //        this.Dispatcher.BeginInvoke(delegate()
            //        {
            //            txtDebug.Text = focusError.Message;
            //        });
            //    }
            //}
        }

        // Capture the image with a full button press using the hardware shutter button.
        private void OnButtonFullPress(object sender, EventArgs e)
        {
            //if (cam != null)
            //{
            //    cam.CaptureImage();
            //}
        }

        // Cancel the focus if the half button press is released using the hardware shutter button.
        private void OnButtonRelease(object sender, EventArgs e)
        {

            //if (cam != null)
            //{
            //    cam.CancelFocus();
            //}
        }


    }
}
