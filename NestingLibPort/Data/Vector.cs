
using ClipperLib;
using System;
using System.Collections.Generic;

namespace NestingLibPort.Data
{
    using Paths = List<List<IntPoint>>;

    public class Vector
    {
        public double x;
        public double y;
        public int id;
        public double rotation;
        public Paths nfp;

        public Vector(double x, double y, int id, double rotation)
        {
            this.x = x;
            this.y = y;
            this.id = id;
            this.rotation = rotation;
            this.nfp = new Paths();
        }

        public Vector(double x, double y, int id, double rotation, Paths nfp)
        {
            this.x = x;
            this.y = y;
            this.id = id;
            this.rotation = rotation;
            this.nfp = nfp;
        }

        public Vector()
        {
            nfp = new Paths();
        }

        
        public override String ToString()
        {
            return "x = " + x + " , y = " + y;
        }
    }

}
