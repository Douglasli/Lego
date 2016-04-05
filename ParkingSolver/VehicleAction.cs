using Gqqnbig.Lego;

namespace Gqqnbig.Lego
{
    public class VehicleAction
    {
        public ActionDirection ActionDirection { get; set; }
        public double Parameter { get; set; }

        public override string ToString()
        {
            return ActionDirection + ", " + Parameter;
        }
    }
}