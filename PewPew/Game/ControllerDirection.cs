using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PewPew.Game
{
    public class ControllerDirection
    {
        public double xy { get; set; }
        public double xz { get; set; }
        public double yz { get; set; }

        public static double CalculateY(double z, double yzAngle)
        {
            return (z / Math.Cos(yzAngle));
        }

        public static double CalculateX(double z, double xzAngle)
        {
            return (z / Math.Cos(xzAngle));
        }
    }
}
