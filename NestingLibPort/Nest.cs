using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NestingLibPort.Data;
using NestingLibPort.Util;
using NestingLibPort.Util.Coor;
using NestingLibPort.Algorithm;
using System.Web.Script.Serialization;

//package com.qunhe.util.nest;

//import com.google.gson.Gson;
//import com.google.gson.GsonBuilder;
//import com.qunhe.util.nest.algorithm.GeneticAlgorithm;
//import com.qunhe.util.nest.algorithm.Individual;
//import com.qunhe.util.nest.data.*;
//import com.qunhe.util.nest.data.Vector;
//import com.qunhe.util.nest.util.*;

//import java.util.*;
namespace NestingLibPort
{

    public class Nest
    {
        private NestPath binPath;
        private List<NestPath> parts;
        private Config config;
        int loopCount;
        private GeneticAlgorithm GA = null;
        private Dictionary<String, List<NestPath>> nfpCache;
        //   private static Gson gson = new GsonBuilder().create();
        private JavaScriptSerializer serialize = new JavaScriptSerializer();
        private int launchcount = 0;

        /**
         *  创建一个新的Nest对象
         * @param binPath   底板多边形
         * @param parts     板件多边形列表
         * @param config    参数设置
         * @param count     迭代计算次数
         */
        public Nest(NestPath binPath, List<NestPath> parts, Config config, int count)
        {
            this.binPath = binPath;
            this.parts = parts;
            this.config = config;
            this.loopCount = count;
            nfpCache = new Dictionary<string, List<NestPath>>();
        }

        /**
         *  开始进行Nest计算
         * @return
         */
        public List<List<Placement>> startNest()
        {
            List<NestPath> tree = CommonUtil.BuildTree(parts, Config.CURVE_TOLERANCE);

            CommonUtil.offsetTree(tree, 0.5 * config.SPACING);
            binPath.config = config;
            foreach (NestPath nestPath in parts)
            {
                nestPath.config = config;
            }
            NestPath binPolygon = NestPath.cleanNestPath(binPath);
            Bound binBound = GeometryUtil.getPolygonBounds(binPolygon);
            if (config.SPACING > 0)
            {
                List<NestPath> offsetBin = CommonUtil.polygonOffset(binPolygon, -0.5 * config.SPACING);
                if (offsetBin.Count == 1)
                {
                    binPolygon = offsetBin[0];
                }
            }
            binPolygon.setId(-1);

            List<int> integers = checkIfCanBePlaced(binPolygon, tree);
            List<NestPath> safeTree = new List<NestPath>();
            foreach (int i in integers)
            {
                safeTree.Add(tree[i]);
            }
            tree = safeTree;

            double xbinmax = binPolygon.get(0).x;
            double xbinmin = binPolygon.get(0).x;
            double ybinmax = binPolygon.get(0).y;
            double ybinmin = binPolygon.get(0).y;

            for (int i = 1; i < binPolygon.size(); i++)
            {
                if (binPolygon.get(i).x > xbinmax)
                {
                    xbinmax = binPolygon.get(i).x;
                }
                else if (binPolygon.get(i).x < xbinmin)
                {
                    xbinmin = binPolygon.get(i).x;
                }

                if (binPolygon.get(i).y > ybinmax)
                {
                    ybinmax = binPolygon.get(i).y;
                }
                else if (binPolygon.get(i).y < ybinmin)
                {
                    ybinmin = binPolygon.get(i).y;
                }
            }
            for (int i = 0; i < binPolygon.size(); i++)
            {
                binPolygon.get(i).x -= xbinmin;
                binPolygon.get(i).y -= ybinmin;
            }


            double binPolygonWidth = xbinmax - xbinmin;
            double binPolygonHeight = ybinmax - ybinmin;

            if (GeometryUtil.polygonArea(binPolygon) > 0)
            {
                binPolygon.reverse();
            }
            /**
             * 确保为逆时针
             */
            for (int i = 0; i < tree.Count; i++)
            {
                Segment start = tree[i].get(0);
                Segment end = tree[i].get(tree[i].size() - 1);
                if (start == end || GeometryUtil.almostEqual(start.x, end.x) && GeometryUtil.almostEqual(start.y, end.y))
                {
                    tree[i].pop();
                }
                if (GeometryUtil.polygonArea(tree[i]) > 0)
                {
                    tree[i].reverse();
                }
            }

            launchcount = 0;
            Result best = null;


            // Tree Modification based on nest4J
            //List<NestPath> modifiedTree = new List<NestPath>();
           
            //for (int i = 0; i < tree.Count; i++)
            //{
            //    List<Segment> modifiedSegment = new List<Segment>();
            //    NestPath currentTree = tree[i];
            //    List<Segment> currentTreeSegments = currentTree.getSegments();
            //    modifiedSegment.Add(currentTreeSegments[currentTreeSegments.Count-1]);
            //    for (int j = 0; j < currentTreeSegments.Count-1; j++)
            //    {
            //        modifiedSegment.Add(currentTreeSegments[j]);
            //    }
            //    currentTree.setSegments(modifiedSegment);
            //    modifiedTree.Add(currentTree);
            //}
            //tree = modifiedTree;




            for (int i = 0; i < loopCount; i++)
            {

                Result result = launchWorkers(tree, binPolygon, config);

                if (i == 0)
                {
                    best = result;
                }
                else
                {
                    if (best.fitness > result.fitness)
                    {
                        best = result;
                    }
                }
            }
            double sumarea = 0;
            double totalarea = 0;
            for (int i = 0; i < best.placements.Count; i++)
            {
                totalarea += Math.Abs(GeometryUtil.polygonArea(binPolygon));
                for (int j = 0; j < best.placements[i].Count; j++)
                {
                    try
                    {
                        sumarea += Math.Abs(GeometryUtil.polygonArea(tree[best.placements[i][j].id]));

                    }
                    catch (Exception ex)
                    {

                       
                    }
                }
            }
            double rate = (sumarea / totalarea) * 100;
            List<List<Placement>> appliedPlacement = applyPlacement(best, tree);
            return appliedPlacement;
        }

        /**
         *  一次迭代计算
         * @param tree  底板
         * @param binPolygon    板件列表
         * @param config    设置
         * @return
         */
        public Result launchWorkers(List<NestPath> tree, NestPath binPolygon, Config config)
        {

            launchcount++;
            if (GA == null)
            {

                List<NestPath> adam = new List<NestPath>();
                foreach (NestPath nestPath in tree)
                {
                    NestPath clone = new NestPath(nestPath);
                    adam.Add(clone);
                }
                foreach (NestPath nestPath in adam)
                {
                    nestPath.area = GeometryUtil.polygonArea(nestPath);
                }
                adam.Sort((x,y)=>x.area.CompareTo(y.area));
                //Collections.sort(adam);
                GA = new GeneticAlgorithm(adam, binPolygon, config);
            }

            Individual individual = null;
            for (int i = 0; i < GA.population.Count; i++)
            {
                if (GA.population[i].getFitness() < 0)
                {
                    individual = GA.population[i];
                    break;
                }
            }
            //        if(individual == null ){
            //            GA.generation();
            //            individual = GA.population.get(1);
            //        }
            if (launchcount > 1 && individual == null)
            {
                GA.generation();
                individual = GA.population[1];
            }

            // 以上为GA

            List<NestPath> placelist = individual.getPlacement();
            List<double> rotations = individual.getRotation();

            List<int> ids = new List<int>();
            for (int i = 0; i < placelist.Count; i++)
            {
                ids.Add(placelist[i].getId());
                placelist[i].setRotation(rotations[i]);
            }
            List<NfpPair> nfpPairs = new List<NfpPair>();
            NfpKey key = null;
            /**
             * 如果在nfpCache里没找到nfpKey 则添加进nfpPairs
             */
            for (int i = 0; i < placelist.Count; i++)
            {
                NestPath part = placelist[i];
                key = new NfpKey(binPolygon.getId(), part.getId(), true, 0, part.getRotation());
                if (!nfpCache.ContainsKey(serialize.Serialize(key)))
                    nfpPairs.Add(new NfpPair(binPolygon, part, key));
                else
                {
                }
                for (int j = 0; j < i; j++)
                {
                    NestPath placed = placelist[j];
                    NfpKey keyed = new NfpKey(placed.getId(), part.getId(), false, rotations[j], rotations[i]);
                    nfpPairs.Add(new NfpPair(placed, part, keyed));
                }
            }


            /**
             * 第一次nfpCache为空 ，nfpCache存的是nfpKey所对应的两个polygon所形成的Nfp( List<NestPath> )
             */
            List<ParallelData> generatedNfp = new List<ParallelData>();
            foreach (NfpPair nfpPair in nfpPairs)
            {
                ParallelData dataTemp = NfpUtil.nfpGenerator(nfpPair, config);
                generatedNfp.Add(dataTemp);
            }
            for (int i = 0; i < generatedNfp.Count; i++)
            {
                ParallelData Nfp = generatedNfp[i];
                //TODO remove gson & generate a new key algorithm
                String tkey = serialize.Serialize(Nfp.getKey()); //gson.toJson(Nfp.getKey());
                if (!nfpCache.ContainsKey(tkey))
                {
                    nfpCache.Add(tkey, Nfp.value);
                }
                else
                {

                }

            }

            PlacementWorker worker = new PlacementWorker(binPolygon, config, nfpCache);
            List<NestPath> placeListSlice = new List<NestPath>();



            for (int i = 0; i < placelist.Count; i++)
            {
                placeListSlice.Add(new NestPath(placelist[i]));
            }
            List<List<NestPath>> data = new List<List<NestPath>>();
            data.Add(placeListSlice);
            List<Result> placements = new List<Result>();
            for (int i = 0; i < data.Count; i++)
            {
                Result result = worker.placePaths(data[i]);
                placements.Add(result);
            }
            if (placements.Count == 0)
            {
                return null;
            }
            individual.fitness = placements[0].fitness;
            Result bestResult = placements[0];
            for (int i = 1; i < placements.Count; i++)
            {
                if (placements[i].fitness < bestResult.fitness)
                {
                    bestResult = placements[i];
                }
            }
            return bestResult;
        }

        /**
         *  通过id与bid将translate和rotate绑定到对应板件上
         * @param best
         * @param tree
         * @return
         */
        public static List<List<Placement>> applyPlacement(Result best, List<NestPath> tree)
        {
            List<List<Placement>> applyPlacement = new List<List<Placement>>();
            for (int i = 0; i < best.placements.Count; i++)
            {
                List<Placement> binTranslate = new List<Placement>();
                for (int j = 0; j < best.placements[i].Count; j++)
                {
                    Vector v = best.placements[i][j];
                    NestPath nestPath = tree[v.id];
                    foreach (NestPath child in nestPath.getChildren())
                    {
                        Placement chPlacement = new Placement(child.bid, new Segment(v.x, v.y), v.rotation);
                        binTranslate.Add(chPlacement);
                    }
                    Placement placement = new Placement(nestPath.bid, new Segment(v.x, v.y), v.rotation);
                    binTranslate.Add(placement);
                }
                applyPlacement.Add(binTranslate);
            }
            return applyPlacement;
        }


        /**
         * 在遗传算法中每次突变或者是交配产生出新的种群时，可能会出现板件与旋转角度不适配的结果，需要重新检查并适配。
         * @param binPolygon
         * @param tree
         * @return
         */
        private static List<int> checkIfCanBePlaced(NestPath binPolygon, List<NestPath> tree)
        {
            List<int> CanBePlacdPolygonIndex = new List<int>();
            Bound binBound = GeometryUtil.getPolygonBounds(binPolygon);
            for (int i = 0; i < tree.Count; i++)
            {
                NestPath nestPath = tree[i];
                if (nestPath.getRotation() == 0)
                {
                    Bound bound = GeometryUtil.getPolygonBounds(nestPath);
                    if (bound.width < binBound.width && bound.height < binBound.height)
                    {
                        CanBePlacdPolygonIndex.Add(i);
                        continue;
                    }
                }
                else
                {
                    for (int j = 0; j < nestPath.getRotation(); j++)
                    {
                        Bound rotatedBound = GeometryUtil.rotatePolygon(nestPath, (360 / nestPath.getRotation()) * j);
                        if (rotatedBound.width < binBound.width && rotatedBound.height < binBound.height)
                        {
                            CanBePlacdPolygonIndex.Add(i);
                            break;
                        }
                    }
                }
            }
            return CanBePlacdPolygonIndex;
        }
       



        public void add(NestPath np)
        {
            parts.Add(np);
        }

        public NestPath getBinPath()
        {
            return binPath;
        }

        public List<NestPath> getParts()
        {
            return parts;
        }

        public void setBinPath(NestPath binPath)
        {
            this.binPath = binPath;
        }

        public void setParts(List<NestPath> parts)
        {
            this.parts = parts;
        }

        public Config getConfig()
        {
            return config;
        }

        public void setConfig(Config config)
        {
            this.config = config;
        }

    }

}
