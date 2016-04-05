using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WpfApplication1
{
    class GridLayer : FrameworkElement
    {
        private readonly Pen pen;
        private readonly Size size;
        private readonly double gridLength;
        private readonly DrawingVisual visual;
        private DrawingContext dc;

        public GridLayer(Pen pen, Size size, double gridLength)
        {
            this.pen = pen;
            this.size = size;
            this.gridLength = gridLength;
            pen.Freeze();

            //this.Loaded += new RoutedEventHandler(DrawIt_Loaded);
            visual = new DrawingVisual();

        }

        //protected override Size MeasureOverride(Size availableSize)
        //{
        //    return solver.Map.Size;
        //}


        protected override Visual GetVisualChild(int index)
        {
            return visual;
        }

        protected override int VisualChildrenCount { get { return 1; } }

        public void InitDrawing()
        {
            dc = visual.RenderOpen();


            for (double i = 0; i < size.Width; i += gridLength)
                dc.DrawLine(pen, new Point(i, 0), new Point(i, size.Height));


            for (double j = 0; j < size.Height; j += gridLength)
            {
                dc.DrawLine(pen, new Point(0, j), new Point(size.Width, j));
            }

        }


        public void SetPoint(int x, int y, bool isOpen = true)
        {
            if (isOpen)
            {
                //画阴影线
                double thickness = gridLength / 10;

                Pen p = new Pen(pen.Brush, thickness);

                for (double i = thickness; i < gridLength; i += (int)(2 * thickness))
                {

                    dc.DrawLine(p, new Point(x + i, y), new Point(x , y + i));

                }


            }
            else
                dc.DrawRectangle(pen.Brush, pen, new Rect(x, y, gridLength, gridLength));
        }

        public void EndDrawing()
        {
            dc.Close();
        }
    }
}
