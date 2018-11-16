using ClipperLib;
using NestingLibPort.Data;
using NestingLibPort.Util.Coor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;


namespace NestingLibPort.Util
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    public class PlacementWorker
    {

        public NestPath binPolygon;
        public Config config;
        public Dictionary<String, List<NestPath>> nfpCache;
        //private static Gson gson = new GsonBuilder().create();
        /**
         *
         * @param binPolygon    底板参数
         * @param config    设置
         * @param nfpCache  nfp列表
         */
        public PlacementWorker(NestPath binPolygon, Config config, Dictionary<String, List<NestPath>> nfpCache)
        {
            this.binPolygon = binPolygon;
            this.config = config;
            this.nfpCache = nfpCache;
        }

        /**
         * 根据板件列表与旋转角列表，通过nfp,计算板件在底板上的位置，并返回这个种群的fitness
         * @param paths
         * @return
         */
        public Result placePaths(List<NestPath> paths)
        {
            List<NestPath> rotated = new List<NestPath>();
            for (int i = 0; i < paths.Count; i++)
            {
                NestPath r = GeometryUtil.rotatePolygon2Polygon(paths[i], paths[i].getRotation());
                r.setRotation(paths[i].getRotation());
                r.setSource(paths[i].getSource());
                r.setId(paths[i].getId());
                rotated.Add(r);
            }
            paths = rotated;

            List<List<Vector>> allplacements = new List<List<Vector>>();
            double fitness = 0;
            double binarea = Math.Abs(GeometryUtil.polygonArea(this.binPolygon));
            String key = null;
            List<NestPath> nfp = null;

            while (paths.Count > 0)
            {

                List<NestPath> placed = new List<NestPath>();
                List<Vector> placements = new List<Vector>();

                fitness += 1;
                double minwidth = Double.MaxValue;
                for (int i = 0; i < paths.Count; i++)
                {

                    NestPath path = paths[i];

                    //inner NFP
                    key = new JavaScriptSerializer().Serialize(new NfpKey(-1, path.getId(), true, 0, path.getRotation()));

                    //key = gson.toJson(new NfpKey(-1, path.getId(), true, 0, path.getRotation()));

                    if (!nfpCache.ContainsKey(key))
                    {
                        continue;
                    }

                    List<NestPath> binNfp = nfpCache[key];



                    // ensure exists
                    bool error = false;
                    for (int j = 0; j < placed.Count; j++)
                    {
                        key = new JavaScriptSerializer().Serialize(new NfpKey(placed[j].getId(), path.getId(), false, placed[j].getRotation(), path.getRotation()));
                        // key = gson.toJson(new NfpKey(placed[j].getId(), path.getId(), false, placed[j].getRotation(), path.getRotation()));
                        if (nfpCache.ContainsKey(key)) nfp = nfpCache[key];
                        else
                        {
                            error = true;
                            break;
                        }
                    }
                    if (error)
                    {
                        continue;
                    }


                    Vector position = null;
                    if (placed.Count == 0)
                    {
                        // first placement , put it on the lefth
                        for (int j = 0; j < binNfp.Count; j++)
                        {
                            for (int k = 0; k < binNfp[j].size(); k++)
                            {
                                if (position == null || binNfp[j].get(k).x - path.get(0).x < position.x)
                                {
                                    position = new Vector(
                                            binNfp[j].get(k).x - path.get(0).x,
                                            binNfp[j].get(k).y - path.get(0).y,
                                            path.getId(),
                                            path.getRotation()
                                    );
                                }
                            }
                        }
                        placements.Add(position);
                        placed.Add(path);
                        continue;
                    }

                    Paths clipperBinNfp = new Paths();

                    for (int j = 0; j < binNfp.Count; j++)
                    {
                        NestPath binNfpj = binNfp[j];
                        clipperBinNfp.Add(scaleUp2ClipperCoordinates(binNfpj));
                    }
                    Clipper clipper = new Clipper();
                    Paths combinedNfp = new Paths();


                    for (int j = 0; j < placed.Count; j++)
                    {

                        key = new JavaScriptSerializer().Serialize(new NfpKey(placed[j].getId(), path.getId(), false, placed[j].getRotation(), path.getRotation()));
                        //key = gson.toJson(new NfpKey(placed[j].getId(), path.getId(), false, placed[j].getRotation(), path.getRotation()));
                        nfp = nfpCache[key];
                        if (nfp == null)
                        {
                            continue;
                        }

                        for (int k = 0; k < nfp.Count; k++)
                        {
                            Path clone =PlacementWorker.scaleUp2ClipperCoordinates(nfp[k]);
                            for (int m = 0; m < clone.Count; m++)
                            {
                                long clx = (long)clone[m].X;
                                long cly = (long)clone[m].Y;
                                IntPoint intPoint=clone[m];
                                intPoint.X = (clx + (long)(placements[j].x * Config.CLIIPER_SCALE));
                                intPoint.Y=(cly + (long)(placements[j].y * Config.CLIIPER_SCALE));
                                clone[m] = intPoint;
                            }
                            //clone = clone.Cleaned(0.0001 * Config.CLIIPER_SCALE);
                            clone = Clipper.CleanPolygon(clone, 0.0001 * Config.CLIIPER_SCALE);
                            double areaPoly = Math.Abs(Clipper.Area(clone));
                            if (clone.Count > 2 && areaPoly > 0.1 * Config.CLIIPER_SCALE * Config.CLIIPER_SCALE)
                            {
                                clipper.AddPath(clone, PolyType.ptSubject,true);
                            }
                        }
                    }
                    if (!clipper.Execute(ClipType.ctUnion, combinedNfp, PolyFillType.pftNonZero, PolyFillType.pftNonZero))
                    {
                        continue;
                    }

                    //difference with bin polygon
                    Paths finalNfp = new Paths();
                    clipper = new Clipper();

                    clipper.AddPaths(combinedNfp, PolyType.ptClip,true);
                    clipper.AddPaths(clipperBinNfp, PolyType.ptSubject,true);
                    if (!clipper.Execute(ClipType.ctDifference, finalNfp, PolyFillType.pftNonZero, PolyFillType.pftNonZero))
                    {
                        continue;
                    }

                    // finalNfp = finalNfp.Cleaned(0.0001 * Config.CLIIPER_SCALE);
                    finalNfp = Clipper.CleanPolygons(finalNfp, 0.0001 * Config.CLIIPER_SCALE);
                    for (int j = 0; j < finalNfp.Count(); j++)
                    {
                        //double areaPoly = Math.Abs(finalNfp[j].Area);
                        double areaPoly = Math.Abs(Clipper.Area(finalNfp[j]));
                        if (finalNfp[j].Count < 3 || areaPoly < 0.1 * Config.CLIIPER_SCALE * Config.CLIIPER_SCALE)
                        {
                            finalNfp.RemoveAt(j);
                            j--;
                        }
                    }

                    if (finalNfp == null || finalNfp.Count == 0)
                    {
                        continue;
                    }

                    List<NestPath> f = new List<NestPath>();
                    for (int j = 0; j < finalNfp.Count; j++)
                    {
                        f.Add(toNestCoordinates(finalNfp[j]));
                    }

                    List<NestPath> finalNfpf = f;
                    double minarea = Double.MinValue;
                    double minX = Double.MaxValue;
                    NestPath nf = null;
                    double area = Double.MinValue;
                    Vector shifvector = null;
                    for (int j = 0; j < finalNfpf.Count; j++)
                    {
                        nf = finalNfpf[j];
                        if (Math.Abs(GeometryUtil.polygonArea(nf)) < 2)
                        {
                            continue;
                        }
                        for (int k = 0; k < nf.size(); k++)
                        {
                            NestPath allpoints = new NestPath();
                            for (int m = 0; m < placed.Count; m++)
                            {
                                for (int n = 0; n < placed[m].size(); n++)
                                {
                                    allpoints.add(new Segment(placed[m].get(n).x + placements[m].x,
                                                                placed[m].get(n).y + placements[m].y));
                                }
                            }
                            shifvector = new Vector(nf.get(k).x - path.get(0).x,nf.get(k).y - path.get(0).y,path.getId(),path.getRotation(),combinedNfp);
                            for (int m = 0; m < path.size(); m++)
                            {
                                allpoints.add(new Segment(path.get(m).x + shifvector.x, path.get(m).y + shifvector.y));
                            }
                            Bound rectBounds = GeometryUtil.getPolygonBounds(allpoints);

                            area = rectBounds.getWidth() * 2 + rectBounds.getHeight();
                            if (minarea == Double.MinValue
                                    || area < minarea
                                    || (GeometryUtil.almostEqual(minarea, area)
                                    && (minX == Double.MinValue || shifvector.x < minX)))
                            {
                                minarea = area;
                                minwidth = rectBounds.getWidth();
                                position = shifvector;
                                minX = shifvector.x;
                            }
                        }
                    }
                    if (position != null)
                    {

                        placed.Add(path);
                        placements.Add(position);
                    }
                }
                if (minwidth != Double.MinValue)
                {
                    fitness += minwidth / binarea;
                }



                for (int i = 0; i < placed.Count; i++)
                {
                    int index = paths.IndexOf(placed[i]);
                    if (index >= 0)
                    {
                        paths.RemoveAt(index);
                    }
                }

                if (placements != null && placements.Count > 0)
                {
                    allplacements.Add(placements);
                }
                else
                {
                    break; // something went wrong
                }

            }
            // there were paths that couldn't be placed
            fitness += 2 * paths.Count;
            return new Result(allplacements, fitness, paths, binarea);
        }


        /**
         *  坐标转换，与clipper库交互必须坐标转换
         * @param polygon
         * @return
         */
        public static Path scaleUp2ClipperCoordinates(NestPath polygon)
        {
            Path p = new Path();
            foreach (Segment s in polygon.getSegments())
            {
                ClipperCoor cc = CommonUtil.toClipperCoor(s.x, s.y);
                p.Add(new IntPoint(cc.getX(), cc.getY()));
            }
            return p;
        }

        public static NestPath toNestCoordinates(Path polygon)
        {
            NestPath clone = new NestPath();
            for (int i = 0; i < polygon.Count; i++)
            {
                Segment s = new Segment((double)polygon[i].X / Config.CLIIPER_SCALE, (double)polygon[i].Y / Config.CLIIPER_SCALE);
                clone.add(s);
            }
            return clone;
        }

    }
}
