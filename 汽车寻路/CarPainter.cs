using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApplication1
{
    class CarPainter
    {
        public CarPainter(Canvas canvas)
        {
            Canvas = canvas;
        }

        public Canvas Canvas { get; private set; }

        private void Draw(Car car, bool fill, DoubleCollection strokeDashArray)
        {
            //Polygon polygon = new Polygon();
            //polygon.Stroke = Brushes.LightSeaGreen;
            //if (fill)
            //    polygon.Fill = Brushes.LightSeaGreen;
            //polygon.StrokeDashArray = strokeDashArray;

            //polygon.Points.Add(car.GetLeftFrontCorner());
            //polygon.Points.Add(car.GetRightFrontCorner());
            //polygon.Points.Add(car.GetRightBackCorner());
            //polygon.Points.Add(car.GetLeftBackCorner());

            //Canvas.Children.Add(polygon);
            Geometry geo = car.GetBound();
            Path path = new Path();
            path.Stroke = Brushes.LightSeaGreen;
            path.StrokeDashArray = strokeDashArray;
            path.Data = geo;
            Canvas.Children.Add(path);


            Line arrowLine1 = new Line();
            arrowLine1.Stroke = Brushes.Red;
            arrowLine1.StrokeThickness = 2;
            arrowLine1.StrokeDashArray = strokeDashArray;
            arrowLine1.X1 = car.Center.X;
            arrowLine1.Y1 = car.Center.Y;

            Matrix m = Matrix.Identity;
            m.Rotate(car.Orientation);
            m.Translate(car.Center.X, car.Center.Y);

            double arrowLength = car.Length / 2 / 3 * 2;
            Point p = new Point(0, arrowLength);
            p = m.Transform(p);
            arrowLine1.X2 = p.X;
            arrowLine1.Y2 = p.Y;

            Canvas.Children.Add(arrowLine1);

            Point pl = new Point(-car.Width / 2 / 4, arrowLength - car.Width / 2 / 3);
            pl = m.Transform(pl);
            Line arrowLine2 = new Line();
            arrowLine2.Stroke = Brushes.Red;
            arrowLine2.StrokeThickness = 2;
            arrowLine2.StrokeDashArray = strokeDashArray;
            arrowLine2.X1 = p.X;
            arrowLine2.Y1 = p.Y;
            arrowLine2.X2 = pl.X;
            arrowLine2.Y2 = pl.Y;
            Canvas.Children.Add(arrowLine2);

            Point pr = new Point(car.Width / 2 / 4, arrowLength - car.Width / 2 / 3);
            pr = m.Transform(pr);
            Line arrowLine3 = new Line();
            arrowLine3.Stroke = Brushes.Red;
            arrowLine3.StrokeThickness = 2;
            arrowLine3.StrokeDashArray = strokeDashArray;
            arrowLine3.X1 = p.X;
            arrowLine3.Y1 = p.Y;
            arrowLine3.X2 = pr.X;
            arrowLine3.Y2 = pr.Y;
            Canvas.Children.Add(arrowLine3);
        }

        internal void Draw(Car car)
        {
            Draw(car, true, null);
        }

        internal void Draw(Map map)
        {
            Canvas.Width = map.Size.Width;
            Canvas.Height = map.Size.Height;
            Polygon polygon = new Polygon();
            polygon.Points.Add(new Point(0, 0));
            polygon.Points.Add(new Point(map.Size.Width, 0));
            polygon.Points.Add(new Point(map.Size.Width, map.Size.Height));
            polygon.Points.Add(new Point(0, map.Size.Height));

            polygon.Stroke = Brushes.Black;
            polygon.StrokeThickness = 1;
            Canvas.Children.Add(polygon);

            foreach (Geometry geometry in map.Fixtures)
            {
                Path path = new Path();
                path.Fill = Brushes.Gold;
                path.Stroke = Brushes.Black;
                path.StrokeThickness = 1;
                path.Data = geometry;
                Canvas.Children.Add(path);
            }
        }

        internal void DrawEndPose(Car car)
        {
            Draw(car, false, new DoubleCollection(new double[] {2}));
        }
    }
}
