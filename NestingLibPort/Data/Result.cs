using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NestingLibPort.Data
{

    public class Result
    {
        public List<List<Vector>> placements;
        public double fitness;
        public List<NestPath> paths;
        public double area;

        public Result(List<List<Vector>> placements, double fitness, List<NestPath> paths, double area)
        {
            this.placements = placements;
            this.fitness = fitness;
            this.paths = paths;
            this.area = area;
        }

        public Result()
        {
        }
    }
}
