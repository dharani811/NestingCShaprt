using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestingLibPort.Util.Coor
{
    public class NestCoor
    {
        public double x;
        public double y;

        public NestCoor()
        {
        }

        public NestCoor(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public double getX()
        {
            return x;
        }

        public void setX(double x)
        {
            this.x = x;
        }

        public double getY()
        {
            return y;
        }

        public void setY(double y)
        {
            this.y = y;
        }
    }
}
