using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Gqqnbig.Lego
{
    public class Map
    {
        public Map(int width, int height, RectangleGeometry[] fixtures = null)
        {
            Size = new Size(width, height);

            if (fixtures != null)
            {
                Fixtures = new List<RectangleGeometry>(fixtures);

                foreach (var geometry in Fixtures)
                {
                    geometry.Freeze();
                }
            }
            else
                Fixtures = new List<RectangleGeometry>();
        }




        public Size Size { get; private set; }

        public ICollection<RectangleGeometry> Fixtures { get; private set; }

    }
}
