using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using MonoBrick.EV3;
using Point = System.Windows.Point;

namespace Gqqnbig.Lego
{
    /// <summary>
    /// 表示一辆真实的汽车。
    /// </summary>
    class CameraVehicle : CameraVehicleBase
    {
        private Brick<IRSensor, Sensor, Sensor, Sensor> brick;

        private Motor leftMotor;
        private Motor rightMotor;


        public IRSensor FrontIRSensor { get; private set; }

        private ServiceHost serviceHost;


        public double SteeringAngle { get; private set; }

        /// <summary>
        /// 获取汽车中点的速度，单位厘米每秒。
        /// </summary>
        public double MidSpeed { get; private set; }


        CalibratedCamera calibratedCamera = new CalibratedCamera();

        public void ConnectToBrick(string method)
        {
            Debug.WriteLine("创建线程：" + System.Threading.Thread.CurrentThread.GetHashCode());

            brick = new Brick<IRSensor, Sensor, Sensor, Sensor>(method);
            brick.Connection.Open();
            brick.Sensor1.Mode = IRMode.Proximity;
            brick.Sensor1.Initialize();
            FrontIRSensor = brick.Sensor1;


            //steeringMotor = brick.MotorC;
            //brick.MotorSync.BitField = OutputBitfield.OutA | OutputBitfield.OutD;
            //speedMotr = brick.MotorSync;
            leftMotor = brick.MotorD;
            //leftMotor.Reverse = true;
            rightMotor = brick.MotorA;
            //rightMotor.Reverse = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        /// <exception cref="AddressAccessDeniedException">当程序不是以管理员权限运行时引发异常。</exception>
        public void ListenToPhone(string ip, int port = 8000)
        {

            var identity = WindowsIdentity.GetCurrent();
            var pr = new WindowsPrincipal(identity);
            if (pr.IsInRole(WindowsBuiltInRole.Administrator) == false)
                throw new AddressAccessDeniedException("Program without Administrator privilege is unable to publish service. \nPlease restart with Administrator privilege.");

            //NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);//WP不支持！
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = 2512000; //2500KB

            serviceHost = new ServiceHost(typeof(PhoneService));
            PhoneService.ImageReceived += PhoneService_ImageReceived;

            string baseAddress = String.Format("http://{0}:{1}/phone", ip, port);
            serviceHost.AddServiceEndpoint(typeof(IPhoneService), binding, baseAddress);
            var behavior = serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>();
            if (behavior == null)
            {
                behavior = new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true };
                serviceHost.Description.Behaviors.Add(behavior);
            }
            behavior.IncludeExceptionDetailInFaults = true;

            ServiceMetadataBehavior bahavior2 = serviceHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (bahavior2 == null)
            {
                bahavior2 = new ServiceMetadataBehavior();
                serviceHost.Description.Behaviors.Add(bahavior2);
            }
            bahavior2.HttpGetEnabled = true;
            bahavior2.HttpGetUrl = new Uri(baseAddress + "/metadata");

            serviceHost.Open();
        }

        private void PhoneService_ImageReceived(object sender, ImageReceivedEventArgs e)
        {
            Image<Bgr, byte> img = new Image<Bgr, byte>(e.Width, e.Height);
            for (int y = 0; y < e.Height; y++)
                for (int x = 0; x < e.Width; x++)
                    for (int ch = 0; ch < 3; ch++)
                        img.Data[y, x, ch] = e.ImageData[y * e.Width * 4 + x * 4 + ch];


            img = calibratedCamera.GetCorrectImage(img);
            img = img.Copy(new MCvBox2D(new PointF(320f, 240f), new SizeF(600, 400), 0));

            OnImageAvailable(new ImageAvailableEventArgs { Image = img, Tick = e.Tick });
        }

        //public override Vehicle Forward(double distance)
        //{
        //    SetSpeed(4.05);

        //    base.Forward(distance);
        //}


        /// <summary>
        /// 转向。如果转向角度大于允许的角度，返回false；否则返回true。
        /// </summary>
        /// <param name="steeringAngle"></param>
        /// <returns></returns>
        public bool Steer(double steeringAngle)
        {
            if (steeringAngle == this.SteeringAngle)
                return true;

            if (Math.Abs(steeringAngle) > State.TurnMaxDegree)
                return false;

            SteeringAngle = steeringAngle;
            MoveStateChanged();
            return true;
        }

        public bool SetSpeed(double speed)
        {

            if (MidSpeed == speed)
                return true;

            if (Math.Abs(speed) > MaxSpeed)
                return false;

            MidSpeed = speed;
            MoveStateChanged();
            return true;
        }

        /// <summary>
        /// 设置速度为speed，经过duration毫秒后，恢复到0。
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public async Task<bool> SetSpeed(double speed, int duration)
        {

            if (MidSpeed == speed)
                return true;

            if (Math.Abs(speed) > MaxSpeed)
                return false;

            MidSpeed = speed;
            MoveStateChanged();

            await Task.Delay(duration);
            SetSpeed(0);
            return true;
        }


        private void MoveStateChanged()
        {
            double leftSpeed, rightSpeed;
            GetRearWheelSpeeds(SteeringAngle, MidSpeed, out leftSpeed, out rightSpeed);

            MotorSpeedChangedEventArgs e = new MotorSpeedChangedEventArgs
            {
                Tick = DateTime.UtcNow.GetUnixTimestamp(),
                LeftSpeed = leftSpeed,
                RightSpeed = rightSpeed
            };


            Debug.WriteLine("调用线程：" + System.Threading.Thread.CurrentThread.GetHashCode());

            leftMotor.On((sbyte)-CalculateMotorPower(leftSpeed));
            rightMotor.On((sbyte)-CalculateMotorPower(rightSpeed));

            base.OnMotorSpeedChanged(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="distance">单位厘米。</param>
        public override async Task Forward(double distance)
        {
            int time = (int)(distance / 4.05 * 1000);
            await SetSpeed(4.05, time);

            await base.Forward(distance);
        }

        public async override Task Backward(double distance)
        {
            if (distance < 0)
                throw new ArgumentException();

            int time = (int)(distance / 4.05 * 1000);
            await SetSpeed(-4.05,time);

            await base.Backward(distance);
        }

        public async override Task ForwardLeft(double degree)
        {
            double centerArcLength = 2 * Math.PI * State.TurningRadius * degree / 360;
            int time = (int)(centerArcLength / 4.05 * 1000);

            Steer(-State.TurnMaxDegree);
            await SetSpeed(4.05, time);
            Steer(0);

            await base.ForwardLeft(degree);
        }

        public async override Task ForwardRight(double degree)
        {
            double centerArcLength = 2 * Math.PI * State.TurningRadius * degree / 360;
            int time = (int)(centerArcLength / 4.05 * 1000);

            Steer(State.TurnMaxDegree);
            await SetSpeed(4.05, time);
            Steer(0);

            await base.ForwardRight(degree);
        }

        public async override Task BackwardLeft(double degree)
        {
            double centerArcLength = 2 * Math.PI * State.TurningRadius * degree / 360;
            int time = (int)(centerArcLength / 4.05 * 1000);

            Steer(-State.TurnMaxDegree);
            await SetSpeed(-4.05, time);
            Steer(0);

            await base.BackwardLeft(degree);
        }

        public async override Task BackwardRight(double degree)
        {
            double centerArcLength = 2 * Math.PI * State.TurningRadius * degree / 360;
            int time = (int)(centerArcLength / 4.05 * 1000);

            Steer(State.TurnMaxDegree);
            await SetSpeed(-4.05, time);
            Steer(0);

            await base.BackwardRight(degree);
        }

        public override void Dispose()
        {
            if (brick != null && brick.Connection.IsConnected)
            {
                leftMotor.Off();
                rightMotor.Off();
                //steeringMotor.On(-10, (uint)Math.Abs(RotationDegree), false);
                brick.Connection.Close();
            }


            if (serviceHost != null && serviceHost.State == CommunicationState.Opened)
            {
                serviceHost.Close();
                PhoneService.ImageReceived -= PhoneService_ImageReceived;
            }
        }

    }
}