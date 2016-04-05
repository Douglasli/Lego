using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApplication1
{
    class Task
    {


        public Task(Point startPoint, double startOrientation, Point endPoint, double endOrientation)
        {
            StartPoint = startPoint;
            StartOrientation = startOrientation;
            EndPoint = endPoint;
            EndOrientation = endOrientation;

            Map = new Map();
            Car = new Car(startPoint,startOrientation);

        }

        public Map Map { get;private set; }

        public Car Car { get;private set; }

        public Point StartPoint { get; private set; }
        public double StartOrientation { get; private set; }

        public Point EndPoint { get; private set; }
        public double EndOrientation { get; private set; }
    }
}
