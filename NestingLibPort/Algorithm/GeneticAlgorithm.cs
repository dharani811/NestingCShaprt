using NestingLibPort.Data;
using NestingLibPort.Util;
using System;
using System.Collections.Generic;


namespace NestingLibPort.Algorithm
{
    public class GeneticAlgorithm
    {
        public List<NestPath> adam;
        public NestPath bin;
        public Bound binBounds;
        public List<double> angles;
        public List<Individual> population;
        public Config config;

        public GeneticAlgorithm(List<NestPath> adam, NestPath bin, Config config)
        {
            this.adam = adam;
            this.bin = bin;
            this.config = config;
            this.binBounds = GeometryUtil.getPolygonBounds(bin);
            population = new List<Individual>();
            init();
        }



        public void generation()
        {
            List<Individual> newpopulation = new List<Individual>();
            population.Sort();

            newpopulation.Add(population[0]);
            while (newpopulation.Count < config.POPULATION_SIZE)
            {
                Individual male = randomWeightedIndividual(null);
                Individual female = randomWeightedIndividual(male);
                List<Individual> children = mate(male, female);
                newpopulation.Add(mutate(children[0]));
                if (newpopulation.Count < population.Count)
                {
                    newpopulation.Add(mutate(children[1]));
                }
            }
            population = newpopulation;
        }

        public List<Individual> mate(Individual male, Individual female)
        {
            List<Individual> children = new List<Individual>();

            long cutpoint =(long) Math.Round(Math.Min(Math.Max(new Random().NextDouble(), 0.1), 0.9) * (male.placement.Count - 1));

            List<NestPath> gene1 = new List<NestPath>();
            List<double> rot1 = new List<double>();
            List<NestPath> gene2 = new List<NestPath>();
            List<double> rot2 = new List<double>();

            for (int i = 0; i < cutpoint; i++)
            {
                gene1.Add(new NestPath(male.placement[i]));
                rot1.Add(male.getRotation()[i]);
                gene2.Add(new NestPath(female.placement[i]));
                rot2.Add(female.getRotation()[i]);
            }

            for (int i = 0; i < female.placement.Count; i++)
            {
                if (!contains(gene1, female.placement[i].getId()))
                {
                    gene1.Add(female.placement[i]);
                    rot1.Add(female.rotation[i]);
                }
            }

            for (int i = 0; i < male.placement.Count; i++)
            {
                if (!contains(gene2, male.placement[i].getId()))
                {
                    gene2.Add(male.placement[i]);
                    rot2.Add(male.rotation[i]);
                }
            }
            Individual individual1 = new Individual(gene1, rot1);
            Individual individual2 = new Individual(gene2, rot2);

            checkAndUpdate(individual1); checkAndUpdate(individual2);


            children.Add(individual1); children.Add(individual2);
            return children;
        }


        private bool contains(List<NestPath> gene, int id)
        {
            for (int i = 0; i < gene.Count; i++)
            {
                if (gene[i].getId() == id)
                {
                    return true;
                }
            }
            return false;
        }

        private Individual randomWeightedIndividual(Individual exclude)
        {
            List<Individual> pop = new List<Individual>();
            for (int i = 0; i < population.Count; i++)
            {
                Individual individual = population[i];
                Individual clone = new Individual(individual);
                pop.Add(clone);
            }
            if (exclude != null)
            {
                int index = pop.IndexOf(exclude);
                if (index >= 0)
                {
                    pop.RemoveAt(index);
                }
            }
            double rand = new Random().NextDouble();
            double lower = 0;
            double weight = 1 / pop.Count;
            double upper = weight;

            for (int i = 0; i < pop.Count; i++)
            {
                if (rand > lower && rand < upper)
                {
                    return pop[i];
                }
                lower = upper;
                upper += 2 * weight * ((pop.Count - i) / pop.Count);
            }
            return pop[0];
        }

        private void init()
        {
            angles = new List<double>();
            for (int i = 0; i < adam.Count; i++)
            {
                double angle = randomAngle(adam[i]);
                angles.Add(angle);
            }
            population.Add(new Individual(adam, angles));
            while (population.Count < config.POPULATION_SIZE)
            {
                Individual mutant = mutate(population[0]);
                population.Add(mutant);
            }
        }

        private Individual mutate(Individual individual)
        {

            Individual clone = new Individual(individual);
            for (int i = 0; i < clone.placement.Count; i++)
            {
                double random = new Random().NextDouble();
                if (random < 0.01 * config.MUTATION_RATE)
                {
                    int j = i + 1;
                    if (j < clone.placement.Count)
                    {
                        var placement = clone.getPlacement();
                        placement.Swap(i, j);
                        //Collections.swap(clone.getPlacement(), i, j);
                    }
                }
                random = new Random().NextDouble();
                if (random < 0.01 * config.MUTATION_RATE)
                {
                   clone.getRotation()[i] = randomAngle(clone.placement[i]);
                 //   clone.getRotation().set(i, randomAngle(clone.placement.get(i)));
                }
            }
            checkAndUpdate(clone);
            return clone;
        }

        /**
         * 为一个polygon 返回一个角度
         * @param part
         * @return
         */
        private double randomAngle(NestPath part)
        {
            List<double> angleList = new List<double>();
            double rotate = Math.Max(1, part.getRotation());
            if (rotate == 0)
            {
                angleList.Add(0);
            }
            else
            {
                for (int i = 0; i < rotate; i++)
                {
                    angleList.Add((360 / rotate) * i);
                }
            }
            angleList.Shuffle();
            //Collections.shuffle(angleList);
            for (int i = 0; i < angleList.Count; i++)
            {
                Bound rotatedPart = GeometryUtil.rotatePolygon(part, angleList[i]);
                if (rotatedPart.getWidth() < binBounds.getWidth() && rotatedPart.getHeight() < binBounds.getHeight())
                {
                    return angleList[i];
                }
            }
            /**
             * 没有找到合法的角度
             */
            return -1;
        }

        public List<NestPath> getAdam()
        {
            return adam;
        }

        public void setAdam(List<NestPath> adam)
        {
            this.adam = adam;
        }

        public NestPath getBin()
        {
            return bin;
        }

        public void setBin(NestPath bin)
        {
            this.bin = bin;
        }

        public void checkAndUpdate(Individual individual)
        {
            for (int i = 0; i < individual.placement.Count; i++)
            {
                double angle = individual.getRotation()[i];
                NestPath nestPath = individual.getPlacement()[i];
                Bound rotateBound = GeometryUtil.rotatePolygon(nestPath, angle);
                if (rotateBound.width < binBounds.width && rotateBound.height < binBounds.height)
                {
                    continue;
                }
                else
                {
                    double safeAngle = randomAngle(nestPath);
                    individual.getRotation()[i]= safeAngle;
                }
            }
        }
    }
}




