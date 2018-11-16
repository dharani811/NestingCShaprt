using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestingLibPort.Data
{

    public class Placement
    {
        public int bid;
        public Segment translate;
        public double rotate;


        public Placement(int bid, Segment translate, double rotate)
        {
            this.bid = bid;
            this.translate = translate;
            this.rotate = rotate;
        }

        public Placement()
        {
        }
    }

}
