using System;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.Lego
{
    class CalibratedCamera
    {
        readonly IntrinsicCameraParameters intrinsicParameters = new IntrinsicCameraParameters();

        public CalibratedCamera()
        {
            intrinsicParameters.DistortionCoeffs = new Matrix<double>(new []{    0.049412396242173383
    ,-1.7099808816543407
    ,-0.0042006885110649084
    ,0.00010671801084137804
    ,2.7745206901701236
    ,-0.064930151867963179
    ,-1.8050527776935834
    ,3.8461046872064184
});
            intrinsicParameters.IntrinsicMatrix = new Matrix<double>(new[,]{
                {656.10251258143194,0.0,322.87002104688321},
                {0.0,650.9036874904358,233.82475090077591},
                {0.0,0.0,1.0}});

        }

        public Image<Bgr,byte> GetCorrectImage(Image<Bgr,byte> img)
        {
            Matrix<float> map1, map2;
            intrinsicParameters.InitUndistortMap(img.Width, img.Height, out map1, out map2);

            //remap the image to the particular intrinsics
            //In the current version of EMGU any pixel that is not corrected is set to transparent allowing the original image to be displayed if the same
            //image is mapped backed, in the future this should be controllable through the flag '0'
            Image<Bgr, Byte> temp = img.CopyBlank();
            CvInvoke.cvRemap(img, temp, map1, map2, 0, new MCvScalar(0));
            return temp;

        }
    }
}
