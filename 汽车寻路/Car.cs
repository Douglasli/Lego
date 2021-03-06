﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WpfApplication1
{
    class Car
    {
        private double m_orientation;
        private Matrix m;

        public Car(Point center, double orientation)
        {
            Center = center;
            Orientation = orientation;

            Qianxuan前悬 = 2;
            Zhouju轴距 = 12.8;
            Houxuan后悬 = 2;
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



        public double TurningRadius
        {
            get
            {

                double cot = 1 / Math.Tan(TurnMaxDegree);
                double backWheelRadius = Zhouju轴距 * cot - Width;

                //旋转中心到汽车中心的距离
                double centerRadius = Math.Sqrt(Math.Pow(backWheelRadius + Width / 2, 2) + Math.Pow(Zhouju轴距 / 2, 2));
                return centerRadius;
            }
        }

        public double Qianxuan前悬 { get; private set; }

        public double Zhouju轴距 { get; private set; }

        public double Houxuan后悬 { get; private set; }

        public double TurnMaxDegree { get; private set; }

        public double Length
        { get { return Qianxuan前悬 + Zhouju轴距 + Houxuan后悬; } }

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

        public Car Forward(double distance)
        {
            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);//相当于平移矩阵乘以旋转矩阵。

            Point nc = m.Transform(new Point(0, distance));
            return new Car(nc, Orientation);

        }


        public Car Backward(double distance)
        {
            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);//相当于平移矩阵乘以旋转矩阵。

            Point nc = m.Transform(new Point(0, -distance));
            return new Car(nc, Orientation);

        }


        public Point GetRightTurnCenter()
        {
            double cot = 1 / Math.Tan(TurnMaxDegree);
            double backWheelRadius = Zhouju轴距 * cot - Width;

            //旋转中心到汽车中心的距离
            double centerRadius = Math.Sqrt(Math.Pow(backWheelRadius + Width / 2, 2) + Math.Pow(Zhouju轴距 / 2, 2));

            //double tx = Center.X - Width / 2 - backWheelRadius;
            //double ty = Center.Y - Zhouju轴距 / 2;

            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);

            Point turnCenter = m.Transform(new Point(-Width / 2 - backWheelRadius, -Zhouju轴距 / 2));
            return turnCenter;
        }

        public Point GetLeftTurnCenter()
        {
            double cot = 1 / Math.Tan(TurnMaxDegree);
            double backWheelRadius = Zhouju轴距 * cot - Width;

            //旋转中心到汽车中心的距离
            //double centerRadius = Math.Sqrt(Math.Pow(backWheelRadius + Width / 2, 2) + Math.Pow(Zhouju轴距 / 2, 2));

            //double tx = Center.X - Width / 2 - backWheelRadius;
            //double ty = Center.Y - Zhouju轴距 / 2;

            //Matrix m = Matrix.Identity;
            //m.Rotate(Orientation);
            //m.Translate(Center.X, Center.Y);

            Point turnCenter = m.Transform(new Point(Width / 2 + backWheelRadius, -Zhouju轴距 / 2));
            return turnCenter;
        }


        public Car ForwardRight(double degree)
        {
            var turnCenter = GetRightTurnCenter();

            Vector v = Center - turnCenter;
            Matrix m = Matrix.Identity;
            m.Rotate(degree);
            v = m.Transform(v);
            Point nc = v + turnCenter;

            return new Car(nc, Orientation + degree);

        }


        public Car ForwardLeft(double degree)
        {
            var turnCenter = GetLeftTurnCenter();

            Vector v = Center - turnCenter;
            Matrix m = Matrix.Identity;
            m.Rotate(-degree);
            v = m.Transform(v);
            Point nc = v + turnCenter;

            return new Car(nc, Orientation - degree);

        }

        public Car BackwardLeft(double degree)
        {
            var turnCenter = GetLeftTurnCenter();

            Vector v = Center - turnCenter;
            Matrix m = Matrix.Identity;
            m.Rotate(degree);
            v = m.Transform(v);
            Point nc = v + turnCenter;

            return new Car(nc, Orientation + degree);
        }

        public Car BackwardRight(double degree)
        {
            var turnCenter = GetRightTurnCenter();

            Vector v = Center - turnCenter;
            Matrix m = Matrix.Identity;
            m.Rotate(-degree);
            v = m.Transform(v);
            Point nc = v + turnCenter;

            return new Car(nc, Orientation - degree);
        }
    }

    enum ActionDirection
    {
        Forward,
        ForwardLeft,
        ForwardRight,
        Backward,
        BackwardLeft,
        BackwardRight
    }
}
