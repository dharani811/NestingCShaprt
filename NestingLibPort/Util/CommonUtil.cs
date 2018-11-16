using NestingLibPort.Data;
using NestingLibPort.Util.Coor;
using System;
using System.Collections.Generic;
using System.Linq;
using ClipperLib;


namespace NestingLibPort.Util
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    public class CommonUtil
    {


        public static NestPath Path2NestPath(Path path)
        {
            NestPath nestPath = new NestPath();
            for (int i = 0; i < path.Count; i++)
            {
                IntPoint lp = path[i];
                NestCoor coor = CommonUtil.toNestCoor(lp.X, lp.Y);
                nestPath.add(new Segment(coor.getX(), coor.getY()));
            }
            return nestPath;
        }

        public static Path NestPath2Path(NestPath nestPath)
        {
            Path path = new Path();
            foreach (Segment s in nestPath.getSegments())
            {
                ClipperCoor coor = CommonUtil.toClipperCoor(s.getX(), s.getY());
                var lp = new IntPoint(coor.getX(), coor.getY());
                path.Add(lp);
            }
            return path;
        }

        /**
         * 坐标转换
         * @param x
         * @param y
         * @return
         */
        public static ClipperCoor toClipperCoor(double x, double y)
        {
            return new ClipperCoor((long)(x * Config.CLIIPER_SCALE), (long)(y * Config.CLIIPER_SCALE));
        }

        /**
         * 坐标转换
         * @param x
         * @param y
         * @return
         */
        public static NestCoor toNestCoor(long x, long y)
        {
            return new NestCoor(((double)x / Config.CLIIPER_SCALE), ((double)y / Config.CLIIPER_SCALE));
        }


        /**
         * 为Clipper下的Path添加点
         * @param x
         * @param y
         * @param path
         */
        private static void addPoint(long x, long y, Path path)
        {
            IntPoint ip = new IntPoint(x, y);
            path.Add(ip);
        }


        /**
         * binPath是作为底板的NestPath , polys则为板件的Path列表
         * 这个方法是为了将binPath和polys在不改变自身形状，角度的情况下放置在一个坐标系内，保证两两之间不交叉
         * @param binPath
         * @param polys
         */
        public static void ChangePosition(NestPath binPath, List<NestPath> polys)
        {

        }

        /**
         *  将NestPath列表转换成父子关系的树
         * @param list
         * @param idstart
         * @return
         */
        public static int toTree(List<NestPath> list, int idstart)
        {
            List<NestPath> parents = new List<NestPath>();
            int id = idstart;
            /**
             * 找出所有的内回环
             */
            for (int i = 0; i < list.Count; i++)
            {
                NestPath p = list[i];
                bool isChild = false;
                for (int j = 0; j < list.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    if (GeometryUtil.pointInPolygon(p.getSegments()[0], list[j]) == true)
                    {
                        list[j].getChildren().Add(p);
                        p.setParent(list[j]);
                        isChild = true;
                        break;
                    }
                }
                if (!isChild)
                {
                    parents.Add(p);
                }
            }
            /**
             *  将内环从list列表中去除
             */
            for (int i = 0; i < list.Count; i++)
            {
                if (parents.IndexOf(list[i]) < 0)
                {
                    list.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < parents.Count; i++)
            {
                parents[i].setId(id);
                id++;
            }

            for (int i = 0; i < parents.Count; i++)
            {
                if (parents[i].getChildren().Count > 0)
                {
                    id = toTree(parents[i].getChildren(), id);
                }
            }
            return id;
        }

        public static NestPath clipperToNestPath(Path polygon)
        {
            
            NestPath normal = new NestPath();
            for (int i = 0; i < polygon.Count; i++)
            {
                NestCoor nestCoor = toNestCoor(polygon[i].X, polygon[i].Y);
                normal.add(new Segment(nestCoor.getX(), nestCoor.getY()));
            }
            return normal;
        }

        public static void offsetTree(List<NestPath> t, double offset)
        {
            
            for (int i = 0; i < t.Count; i++)
            {
                List<NestPath> offsetPaths = polygonOffset(t[i], offset);
                if (offsetPaths.Count == 1)
                {
                    t[i].clear();
                    NestPath from = offsetPaths[0];

                    foreach (Segment s in from.getSegments())
                    {
                        t[i].add(s);
                    }
                }
                if (t[i].getChildren().Count > 0)
                {

                    offsetTree(t[i].getChildren(), -offset);
                }
            }
        }

        public static List<NestPath> polygonOffset(NestPath polygon, double offset)
        {
            List<NestPath> result = new List<NestPath>();
            if (offset == 0 || GeometryUtil.almostEqual(offset, 0))
            {
                /**
                 * return EmptyResult
                 */
                return result;
            }
            Path p = new Path();
            foreach (Segment s in polygon.getSegments())
            {
                ClipperCoor cc = toClipperCoor(s.getX(), s.getY());
                p.Add(new IntPoint(cc.getX(), cc.getY()));
            }

            int miterLimit = 2;
            ClipperOffset co = new ClipperOffset(miterLimit, Config.CURVE_TOLERANCE * Config.CLIIPER_SCALE);
            co.AddPath(p, JoinType.jtRound, EndType.etClosedPolygon);

            Paths newpaths = new Paths();
            co.Execute(ref newpaths, offset * Config.CLIIPER_SCALE);


            /**
             * 这里的length是1的话就是我们想要的
             */
            for (int i = 0; i < newpaths.Count; i++)
            {
                result.Add(CommonUtil.clipperToNestPath(newpaths[i]));
            }

            if (offset > 0)
            {
                NestPath from = result[0];
                if (GeometryUtil.polygonArea(from) > 0)
                {
                    from.reverse();
                }
                from.add(from.get(0)); from.getSegments().RemoveAt(0);
            }


            return result;
        }


        /**
         * 对应于JS项目中的getParts
         */
        public static List<NestPath> BuildTree(List<NestPath> parts, double curve_tolerance)
        {
            List<NestPath> polygons = new List<NestPath>();
            for (int i = 0; i < parts.Count; i++)
            {
                NestPath cleanPoly = NestPath.cleanNestPath(parts[i]);
                cleanPoly.bid = parts[i].bid;
                if (cleanPoly.size() > 2 && Math.Abs(GeometryUtil.polygonArea(cleanPoly)) > curve_tolerance * curve_tolerance)
                {
                    cleanPoly.setSource(i);

                    polygons.Add(cleanPoly);
                }
            }

            CommonUtil.toTree(polygons, 0);
            return polygons;
        }
    }
}
