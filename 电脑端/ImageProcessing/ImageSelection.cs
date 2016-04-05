using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.Lego.ImageProcessing
{
    class ImageSelection
    {
        private Image<Bgr, byte> image;


        public ImageSelection(Image<Bgr, byte> image, System.Drawing.Rectangle rect)
        {
            this.image = image; 
        }
    }
}
