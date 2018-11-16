using NestingLibPort.Data;
using System;
using System.Collections.Generic;

//package com.qunhe.util.nest.algorithm;

//import com.qunhe.util.nest.data.NestPath;

//import java.util.ArrayList;
//import java.util.List;


namespace NestingLibPort.Algorithm
{
    public class Individual:IComparable<Individual>
    {
        public List<NestPath> placement;
        public List<double> rotation;
        public double fitness;


        public Individual(Individual individual)
        {
            fitness = individual.fitness;
            placement = new List<NestPath>();
            rotation = new List<double>();
            for (int i = 0; i < individual.placement.Count; i++)
            {
                NestPath cloneNestPath = new NestPath(individual.placement[i]);
                placement.Add(cloneNestPath);
            }
            for (int i = 0; i < individual.rotation.Count; i++)
            {
                double rotationAngle = individual.getRotation()[i];
                rotation.Add(rotationAngle);
            }
        }


        public Individual()
        {
            fitness = -1;
            placement = new List<NestPath>();
            rotation = new List<double>();
        }

        public Individual(List<NestPath> placement, List<double> rotation)
        {
            fitness = -1;
            this.placement = placement;
            this.rotation = rotation;
        }

        public int size()
        {
            return placement.Count;
        }

        public List<NestPath> getPlacement()
        {
            return placement;
        }

        public void setPlacement(List<NestPath> placement)
        {
            this.placement = placement;
        }

        public List<double> getRotation()
        {
            return rotation;
        }

        public void setRotation(List<double> rotation)
        {
            this.rotation = rotation;
        }


        public  int CompareTo(Individual o)
        {
            if (fitness > o.fitness)
            {
                return 1;
            }
            else if (fitness == o.fitness)
            {
                return 0;
            }
            return -1;
        }


        
    public  bool Equals(Object obj)
        {
            Individual individual = (Individual)obj;
            if (placement.Count != individual.size())
            {
                return false;
            }
            for (int i = 0; i < placement.Count; i++)
            {
                if (!placement[i].Equals(individual.getPlacement()[i]))
                {
                    return false;
                }
            }
            if (rotation.Count != individual.getRotation().Count)
            {
                return false;
            }
            for (int i = 0; i < rotation.Count; i++)
            {
                if (rotation[i] != individual.getRotation()[i])
                {
                    return false;
                }
            }
            return true;
        }


        
    public override String ToString()
        {
            String res = "";
            int count = 0;
            for (int i = 0; i < placement.Count; i++)
            {
                res += "NestPath " + count + "\n";
                count++;
                res += placement[i].ToString() + "\n";
            }
            res += "rotation \n";
            foreach (int r in rotation)
            {
                res += r + " ";
            }
            res += "\n";

            return res;
        }

        public double getFitness()
        {
            return fitness;
        }

        public void setFitness(double fitness)
        {
            this.fitness = fitness;
        }


    }
}
