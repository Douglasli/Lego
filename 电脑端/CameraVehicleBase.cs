using System;
using System.Diagnostics.Contracts;
using System.Windows;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.Lego
{
    abstract class CameraVehicleBase
    {
        //乐高一格0.8cm。

        /// <summary>
        /// 最大实际速度，单位厘米每秒。
        /// </summary>
        public readonly double MaxSpeed = 25;

        /// <summary>
        /// 摄像机相对于后轮轴中点的距离。
        /// <para>X为左右偏移量，负为左，正为右。Y为前后偏移量，正为前，负为后。</para>
        /// </summary>
        public readonly Point CameraPosition = new Point(-5.5, 5);

        public VehicleState State { get; private set; }

        public Point Center { get { return State.Center; } }

        public double Width { get { return State.Width; } }

        public double Length { get { return State.Length; } }



        public CameraVehicleBase()
        {
            State = new VehicleState(new Point(127.25, 214.4), 180);
        }

        /// <summary>
        /// 根据指定的轮胎旋转角，计算旋转中心到后轮轴中点的距离。
        /// </summary>
        /// <param name="steeringAngle">单位为角度。</param>
        /// <returns></returns>
        public double GetSteeringRadius(double steeringAngle)
        {
            Contract.Ensures(Contract.Result<double>() > 0);
            return State.AxisDistance / Math.Tan(Math.Abs(steeringAngle) / 180 * Math.PI);
        }

        /// <summary>
        /// 在转弯时，左右两轮有速度差。
        /// 给定车中线的速度，计算左右轮的速度。
        /// </summary>
        /// <param name="steeringAngle"></param>
        /// <param name="midSpeed"></param>
        /// <param name="leftSpeed"></param>
        /// <param name="rightSpeed"></param>
        public void GetRearWheelSpeeds(double steeringAngle, double midSpeed, out double leftSpeed, out double rightSpeed)
        {
            if (Math.Abs(steeringAngle) < 1)
            {
                leftSpeed = rightSpeed = midSpeed;
                return;
            }

            double radius = GetSteeringRadius(steeringAngle);
            double angularVelocity = midSpeed / radius;

            if (steeringAngle < 0)
            {
                leftSpeed = angularVelocity * (radius - State.Width / 2);
                rightSpeed = angularVelocity * (radius + State.Width / 2);
            }
            else
            {
                leftSpeed = angularVelocity * (radius + State.Width / 2);
                rightSpeed = angularVelocity * (radius - State.Width / 2);
            }
        }

        ///// <summary>
        ///// 返回实际速度，单位厘米每秒。
        ///// </summary>
        ///// <param name="power"></param>
        ///// <returns></returns>
        //public static double GetSpeed(sbyte power)
        //{
        //    if (power == 10)
        //        return 4.05;
        //    throw new ArgumentOutOfRangeException();
        //}

        public static sbyte CalculateMotorPower(double speed)
        {
            if (speed > 0)
                return (sbyte)Math.Round(0.209205 * (803 - 1 * Math.Sqrt(644809 - 18164 * speed)));
            else
                return (sbyte)-Math.Round(0.209205 * (803 - 1 * Math.Sqrt(644809 + 18164 * speed)));
        }


        public event EventHandler<ImageAvailableEventArgs> ImageAvailable;

        /// <summary>
        /// 可能在另一个线程引发此事件。
        /// </summary>
        public event EventHandler<MotorSpeedChangedEventArgs> MotorSpeedChanged;

        protected virtual void OnImageAvailable(ImageAvailableEventArgs e)
        {
            var handler = ImageAvailable;
            if (handler != null)
                handler(this, e);
        }

        protected virtual void OnMotorSpeedChanged(MotorSpeedChangedEventArgs e)
        {
            var handler = MotorSpeedChanged;
            if (handler != null)
                handler(this, e);
        }

        public abstract void Dispose();

        public async virtual System.Threading.Tasks.Task Forward(double distance)
        {
            State = State.Forward(distance);
        }

        public async virtual System.Threading.Tasks.Task Backward(double distance)
        {
            State = State.Backward(distance);
        }

        public async virtual System.Threading.Tasks.Task ForwardLeft(double degree)
        {
            State = State.ForwardLeft(degree);
        }

        public async virtual System.Threading.Tasks.Task ForwardRight(double degree)
        {
            State = State.ForwardRight(degree);
        }

        public async virtual System.Threading.Tasks.Task BackwardLeft(double degree)
        {
            State = State.BackwardLeft(degree);
        }

        public async virtual System.Threading.Tasks.Task BackwardRight(double degree)
        {
            State = State.BackwardRight(degree);
        }

    }

    public class ImageAvailableEventArgs : TimedEventArgs
    {
        public Image<Bgr, byte> Image { get; set; }
    }
}
