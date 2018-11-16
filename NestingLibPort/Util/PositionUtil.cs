using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NestingLibPort.Data;


//package com.qunhe.util.nest.util;

//import com.qunhe.util.nest.data.NestPath;

//import java.util.List;
namespace NestingLibPort.Util
{
   public class PositionUtil
    {

        public static List<NestPath> positionTranslate4Path(double x, double y, List<NestPath> paths)
        {
            foreach (NestPath path in paths)
            {
                path.translate(x, y);
                y = path.getMaxY() + 10;
            }
            return paths;
        }
    }
}
