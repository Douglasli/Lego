using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApplication1
{
    class Map
    {
        public Map()
        {
            Size = new Size(300, 300);

            Fixtures = new List<RectangleGeometry>();
            Fixtures.Add(new RectangleGeometry(new Rect(0, 176, 18, 1)));
            Fixtures.Add(new RectangleGeometry(new Rect(18, 150, 1, 26)));

            Fixtures.Add(new RectangleGeometry(new Rect(0, 150, 18, 0.5)));


            Fixtures.Add(new RectangleGeometry(new Rect(0, 120, 16.5, 1)));
            Fixtures.Add(new RectangleGeometry(new Rect(16.5, 96.4, 1, 23.6)));
            Fixtures.Add(new RectangleGeometry(new Rect(0, 96.4, 16.5, 1)));

            Fixtures.Add(new RectangleGeometry(new Rect(30, 100, 50, 10)));



            foreach (var geometry in Fixtures)
            {
                geometry.Freeze();
            }
        }

        


        public Size Size { get; private set; }

        public ICollection<RectangleGeometry> Fixtures { get; private set; }

    }
}
