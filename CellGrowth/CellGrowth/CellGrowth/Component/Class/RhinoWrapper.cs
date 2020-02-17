using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel.Geometry;

using System.Linq;




namespace myRhinoWrapper
{
    public static class RhinoWrapper
    {
        public static List<Rhino.Geometry.Point3d> MakeGrid(int x_Ex, int y_Ex, int gridSize)
        {
            var rtnList = new List<Rhino.Geometry.Point3d>();

            for (int i = 0; i < y_Ex; i++)
            {
                for (int j = 0; j < x_Ex; j++)
                {
                    rtnList.Add(new Rhino.Geometry.Point3d(j * gridSize, i * gridSize, 0));
                }
            }

            return rtnList;
        }

        public static Rhino.Geometry.Rectangle3d MakeRect(Rhino.Geometry.Point3d origin,
            int x_Ex, int y_Ex, int gridSize, int offset = 0)
        {
            var offsOrigin = new Point3d(origin.X + offset, origin.Y + offset, 0);
            var planeXY = new Rhino.Geometry.Plane(offsOrigin, Rhino.Geometry.Vector3d.ZAxis);

            return new Rhino.Geometry.Rectangle3d(planeXY, (x_Ex * gridSize) - offset * 2, (y_Ex * gridSize) - offset * 2);
        }

        public static List<Rhino.Geometry.Point3d> RandomPt(Rectangle3d rect, int num)
        {
            var rtnList = new List<Rhino.Geometry.Point3d>();

            var myRandomGenerator = new Random();

            for (int i = 0; i < num; i++)
            {
                double x = myRandomGenerator.NextDouble();
                double y = myRandomGenerator.NextDouble();
                double z = 0.0;

                x = Remap(x, 0f, 1f, rect.X.Min, rect.X.Max);
                y = Remap(y, 0f, 1f, rect.Y.Min, rect.Y.Max);
                var myRandomPoint = new Rhino.Geometry.Point3d(x, y, z);
                rtnList.Add(myRandomPoint);
            }

            return rtnList;
        }

        public static double Remap(this double value, double from1, double to1, double from2, double to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }


        public static bool IsInside(Point3d pt, Polyline crv)
        {
            Point3d pt1, pt2;
            bool oddNodes = false;

            for (int i = 0; i < crv.SegmentCount; i++) //for each contour line
            {

                pt1 = crv.SegmentAt(i).From; //get start and end pt
                pt2 = crv.SegmentAt(i).To;

                if ((pt1[1] < pt[1] && pt2[1] >= pt[1] || pt2[1] < pt[1] && pt1[1] >= pt[1]) && (pt1[0] <= pt[0] || pt2[0] <= pt[0])) //if pt is between pts in y, and either of pts is before pt in x
                    oddNodes ^= (pt2[0] + (pt[1] - pt2[1]) * (pt1[0] - pt2[0]) / (pt1[1] - pt2[1]) < pt[0]); //^= is xor
                                                                                                             //end.X + (pt-end).Y   * (start-end).X  /(start-end).Y   <   pt.X
            }


            if (!oddNodes)
            {
                double minDist = 1e10;
                for (int i = 0; i < crv.SegmentCount; i++)
                {
                    Point3d cp = crv.SegmentAt(i).ClosestPoint(pt, true);
                    //Point3d cp = mvContour[i].closestPoint(pt);
                    //minDist = min(minDist, cp.distance(pt));
                    minDist = Math.Min(minDist, cp.DistanceTo(pt));
                }
                if (minDist < 1e-10)
                    return true;
            }

            if (oddNodes) return true;

            return false;
        }

        public static List<Line> GetDelaunayEdge(List<Point3d> pts)
        {
            var nodes = new Node2List();
            foreach (var pt in pts)
            {
                nodes.Append(new Node2(pt.X, pt.Y));
            }

            var faces = Grasshopper.Kernel.Geometry.Delaunay.Solver.Solve_Faces(nodes, 1);
            var delMesh = Grasshopper.Kernel.Geometry.Delaunay.Solver.Solve_Mesh(nodes, 1, ref faces);

            var edgeList = new List<Line>();
            for (int i = 0; i < delMesh.TopologyEdges.Count; i++)
            {
                edgeList.Add(delMesh.TopologyEdges.EdgeLine(i));
            }

            return edgeList;
        }

        public static List<Brep> ConvertPtsToBreps(List<Point3d> pts, int gridSize)
        {
            var rtnList = new List<Brep>();

            var xInterval = new Interval(-(gridSize / 2), gridSize / 2);
            var yInterval = new Interval(-(gridSize / 2), gridSize / 2);


            var breps = new List<Brep>();

            foreach (var pt in pts)
            {
                var planeXY = new Rhino.Geometry.Plane(pt, Vector3d.ZAxis);
                var srf = new PlaneSurface(planeXY, xInterval, yInterval);
                rtnList.Add(srf.ToBrep());
            }

            return rtnList;
        }

        public static Curve BrepNakedEdge(List<Brep> breps)
        {
            Brep brep = Brep.JoinBreps(breps, 1)[0];
            brep.JoinNakedEdges(1);

            var crv = Curve.JoinCurves(brep.DuplicateNakedEdgeCurves(true, false), 1)[0];
            return crv;
        }
        public static void SortList(ref List<Point3d> list)
        {
            var arr = list.ToArray();
            var xArr = arr.Select(pt => pt.X).ToArray();
            var yArr = arr.Select(pt => pt.Y).ToArray();

            System.Array.Sort(arr, xArr);
            System.Array.Sort(arr, yArr);

            list.Clear();
            list.AddRange(arr);
        }

        public static void ConvertToTree(GH_Structure<GH_Point> structure, ref DataTree<Point3d> tree)
        {
            for (int i = 0; i < structure.Paths.Count; i++)
            {
                var path = structure.Paths[i];
                for (int j = 0; j < structure[path].Count; j++)
                {
                    tree.Add(structure[path][j].Value, path);
                }
            }
        }

        public static List<double> DistNearPt(List<Point3d> pts)
        {
            var rtnList = new List<double>();

            for (int i = 0; i < pts.Count; i++)
            {
                var others = new List<Point3d>(pts);
                others.RemoveAt(i);
                var pc = new Rhino.Geometry.PointCloud(others);
                var closestIdx = pc.ClosestPoint(pts[i]);

                rtnList.Add(pts[i].DistanceTo(others[closestIdx]));
            }

            return rtnList;
        }
    }


    public static class RhinoExtension
    {
        public static void Jitter<T>(this IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                var rand = new Random();
                int j = rand.Next(0, i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
