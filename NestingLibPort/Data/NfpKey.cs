using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestingLibPort.Data
{
    public class NfpKey
    {

       public int A;
        public int B;
        public bool inside;
        public double Arotation;
        public double Brotation;

        public NfpKey(int a, int b, bool inside, double arotation, double brotation)
        {
            A = a;
            B = b;
            this.inside = inside;
            Arotation = arotation;
            Brotation = brotation;
        }

        public NfpKey()
        {
        }

        public int getA()
        {
            return A;
        }

        public void setA(int a)
        {
            A = a;
        }

        public int getB()
        {
            return B;
        }

        public void setB(int b)
        {
            B = b;
        }

        public bool isInside()
        {
            return inside;
        }

        public void setInside(bool inside)
        {
            this.inside = inside;
        }

        public double getArotation()
        {
            return Arotation;
        }

        public void setArotation(double arotation)
        {
            Arotation = arotation;
        }

        public double getBrotation()
        {
            return Brotation;
        }

        public void setBrotation(double brotation)
        {
            Brotation = brotation;
        }
    }
}
