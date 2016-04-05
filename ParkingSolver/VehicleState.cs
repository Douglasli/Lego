using System.Windows;
using System.Windows.Media;

namespace Gqqnbig.Lego
{
    /// <summary>
    /// 表示汽车的一个状态，是不可变类。
    /// </summary>
    public class VehicleState
    {
        private double m_orientation;
        private Matrix m;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="center"></param>
        /// <param name="orientation">0是朝正下方。</param>
        public VehicleState(Point center, double orientation)
        {
            Center = center;
            Orientation = orientation;

            Qianxuan前悬 = 2;
            //因为前轮不转动产生摩擦，稍微提前后轮2厘米。
            AxisDistance = 12.8 ;
            Houxuan后悬 = 2 ;
            Width = 15.5;

            TurnMaxDegree = 35;

            m = Matrix.Identity;
            m.Rotate(Orientation);
            m.Translate(Center.X, Center.Y);
        }


        public Point Center { get; private set; }

        /// <summary>角度，0～360</summary>
        public double Orientation
        {
            get
            {
                m_orientation = m_orientation % 360;
                if (m_orientation < 0)
                    m_orientation += 360;

                return m_orientation;
            }
            private set { m_orientation = value; }
        }


        /// <summary>
        /// 旋转中心到汽车中心的距离
        /// </summary>
        public double TurningRadius
        {
            get
            {

                double cot = 1 / System.Math.Tan(TurnMaxDegree);
                double backWheelRadius = AxisDistance * cot - Width;

                //旋转中心到汽车中心的距离
                double centerRadius = System.Math.Sqrt(System.Math.Pow(backWheelRadius + Width / 2, 2) + System.Math.Pow(AxisDistance / 2, 2));
                return centerRadius;
            }
        }

        public double Qianxuan前悬 { get; private set; }

        public double AxisDistance { get; private set; }

        public double Houxuan后悬 { get; private set; }

        public double TurnMaxDegree { get; private set; }

        public double Length
        { get { return Qianxuan前悬 + AxisDistance + Houxuan后悬; } }

        public double Width { get; private set; }

        public Point GetLeftFrontCorner()
        {
            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);

            return m.Transform(new Point(-Width / 2, Length / 2));
        }

        public Point GetRightFrontCorner()
        {
            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);

            return m.Transform(new Point(Width / 2, Length / 2));
        }

        public Point GetLeftBackCorner()
        {


            return m.Transform(new Point(-Width / 2, -Length / 2));
        }

        public RectangleGeometry GetBound()
        {
            RectangleGeometry g = new RectangleGeometry();
            g.Rect = new Rect(new Point(-Width / 2, -Length / 2), new Point(Width / 2, Length / 2));

            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);

            g.Transform = new MatrixTransform(m);
            g.Freeze();
            return g;
        }

        public Point GetRightBackCorner()
        {
            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);

            return m.Transform(new Point(Width / 2, -Length / 2));
        }

        public virtual VehicleState Forward(double distance)
        {
            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);//相当于平移矩阵乘以旋转矩阵。

            Point nc = m.Transform(new Point(0, distance));
            return new VehicleState(nc, Orientation).Copy();

        }


        public virtual VehicleState Backward(double distance)
        {
            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);//相当于平移矩阵乘以旋转矩阵。

            Point nc = m.Transform(new Point(0, -distance));
            return new VehicleState(nc, Orientation).Copy();
        }


        public Point GetRightTurnCenter()
        {
            double cot = 1 / System.Math.Tan(TurnMaxDegree);
            double backWheelRadius = AxisDistance * cot - Width;

            //旋转中心到汽车中心的距离
            double centerRadius = System.Math.Sqrt(System.Math.Pow(backWheelRadius + Width / 2, 2) + System.Math.Pow(AxisDistance / 2, 2));

            //double tx = Center.X - Width / 2 - backWheelRadius;
            //double ty = Center.Y - AxisDistance / 2;

            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);

            Point turnCenter = m.Transform(new Point(-Width / 2 - backWheelRadius, -AxisDistance / 2));
            return turnCenter;
        }

        public Point GetLeftTurnCenter()
        {
            double cot = 1 / System.Math.Tan(TurnMaxDegree);
            double backWheelRadius = AxisDistance * cot - Width;

            //旋转中心到汽车中心的距离
            //double centerRadius = Math.Sqrt(Math.Pow(backWheelRadius + Width / 2, 2) + Math.Pow(AxisDistance / 2, 2));

            //double tx = Center.X - Width / 2 - backWheelRadius;
            //double ty = Center.Y - AxisDistance / 2;

            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);

            Point turnCenter = m.Transform(new Point(Width / 2 + backWheelRadius, -AxisDistance / 2));
            return turnCenter;
        }


        public virtual VehicleState ForwardRight(double degree)
        {
            var turnCenter = GetRightTurnCenter();

            Vector v = Center - turnCenter;
            Matrix m = Matrix.Identity;
            m.Rotate(degree);
            v = m.Transform(v);
            Point nc = v + turnCenter;

            return new VehicleState(nc, Orientation + degree).Copy();

        }


        public virtual VehicleState ForwardLeft(double degree)
        {
            var turnCenter = GetLeftTurnCenter();

            Vector v = Center - turnCenter;
            Matrix m = Matrix.Identity;
            m.Rotate(-degree);
            v = m.Transform(v);
            Point nc = v + turnCenter;

            return new VehicleState(nc, Orientation - degree).Copy();

        }

        public virtual VehicleState BackwardLeft(double degree)
        {
            var turnCenter = GetLeftTurnCenter();

            Vector v = Center - turnCenter;
            Matrix m = Matrix.Identity;
            m.Rotate(degree);
            v = m.Transform(v);
            Point nc = v + turnCenter;

            return new VehicleState(nc, Orientation + degree).Copy();
        }

        public virtual VehicleState BackwardRight(double degree)
        {
            var turnCenter = GetRightTurnCenter();

            Vector v = Center - turnCenter;
            Matrix m = Matrix.Identity;
            m.Rotate(-degree);
            v = m.Transform(v);
            Point nc = v + turnCenter;

            return new VehicleState(nc, Orientation - degree).Copy();
        }

        public virtual VehicleState Copy()
        {
            return new VehicleState(Center, Orientation);
        }
    }

    public enum ActionDirection
    {
        Forward,
        ForwardLeft,
        ForwardRight,
        Backward,
        BackwardLeft,
        BackwardRight
    }
}
