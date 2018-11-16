using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestingLibPort.Data
{

    public class SegmentRelation
    {
        public int type;
        public int A;
        public int B;

        public SegmentRelation(int type, int a, int b)
        {
            this.type = type;
            A = a;
            B = b;
        }

        public SegmentRelation()
        {
        }
    }

}
