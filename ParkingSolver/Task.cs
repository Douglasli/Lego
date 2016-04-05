using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Gqqnbig.Lego;

namespace Gqqnbig.Lego
{
    class Task
    {


        public Task(Point startPoint, double startOrientation, Point endPoint, double endOrientation, Map map)
        {
            StartPoint = startPoint;
            StartOrientation = startOrientation;
            EndPoint = endPoint;
            EndOrientation = endOrientation;

            Map = map;
            Car = new VehicleState(startPoint, startOrientation);

        }

        public Map Map { get; private set; }

        public VehicleState Car { get; private set; }

        public Point StartPoint { get; private set; }
        public double StartOrientation { get; private set; }

        public Point EndPoint { get; private set; }
        public double EndOrientation { get; private set; }
    }
}
