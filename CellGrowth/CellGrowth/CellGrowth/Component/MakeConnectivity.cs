using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel.Geometry;

using System.Linq;
using myRhinoWrapper;

namespace CellGrowth.Component
{
    public class MakeConnectivity : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MakeConnectivity class.
        /// </summary>
        public MakeConnectivity()
          : base("MakeConnectivity", "Nickname",
              "Description",
              "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("AreaCenter", "", "", GH_ParamAccess.list);
            pManager.AddPointParameter("AreaPts", "", "", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Shape", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("GridSize", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("MaxBranch", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Seed", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("ConnectLines", "", "", GH_ParamAccess.list);
            pManager.AddBrepParameter("AreaBreps", "", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var areaCents = new List<Point3d>();
            var areaPtsBuff = new GH_Structure<GH_Point>();
            var areaPts = new DataTree<Point3d>();
            Curve shape = null;
            int gridSize = 0;
            int maxBranch = 0;
            int seed = 0;

            if (!DA.GetDataList(0, areaCents)) return;
            if (!DA.GetDataTree(1, out areaPtsBuff)) return;
            if (!DA.GetData(2, ref shape)) return;
            if (!DA.GetData(3, ref gridSize)) return;
            if (!DA.GetData(4, ref maxBranch)) return;
            if (!DA.GetData(5, ref seed)) return;

            RhinoWrapper.ConvertToTree(areaPtsBuff, ref areaPts);


            var delEdges = RhinoWrapper.GetDelaunayEdge(areaCents);
            delEdges = IntersectShape(shape, delEdges);

            var areaShapes = new List<Curve>();
            for (int i = 0; i < areaPts.Paths.Count; i++)
            {
                var path = areaPts.Paths[i];
                var breps = RhinoWrapper.ConvertPtsToBreps(areaPts.Branch(path), gridSize);
                var brep = Brep.JoinBreps(breps, 1)[0];
                brep.JoinNakedEdges(1);
                var nakedCrv = Curve.JoinCurves(brep.DuplicateNakedEdgeCurves(true, false), 1)[0];
                areaShapes.Add(nakedCrv);
            }

            delEdges = IntersectArea(areaShapes, delEdges);
            var rtnList = SelectLines(areaCents, maxBranch, delEdges, seed);

            DA.SetDataList(0, rtnList);
            DA.SetDataList(1, areaShapes);
        }


        private List<Line> IntersectShape(Curve crv, List<Line> lines)
        {
            var rtnList = new List<Line>();

            var result = lines.Select(line => Rhino.Geometry.Intersect.Intersection.CurveLine(crv, line, 1, 1).Count).ToArray();

            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] == 2)
                {
                    rtnList.Add(lines[i]);
                }
            }
            return rtnList;
        }

        private List<Line> IntersectArea(List<Curve> areaCrvs, List<Line> lines)
        {
            var rtnList = new List<Line>();
            
            var interCounts = new List<int>();
            for (int i = 0; i < lines.Count; i++)
            {
                int count = 0;
                for (int j = 0; j < areaCrvs.Count; j++)
                {
                    var intersectNum = Rhino.Geometry.Intersect.
                      Intersection.CurveCurve(areaCrvs[j], lines[i].ToNurbsCurve(), 0.1, 0.1).Count;
                    count += intersectNum;
                }

                interCounts.Add(count);
            }

            var boolList = new List<bool>();
            foreach (var val in interCounts)
            {
                if (val == 2)
                {
                    boolList.Add(true);
                }
                else
                {
                    boolList.Add(false);
                }
            }

            for (int i = 0; i < boolList.Count; i++)
            {
                if (boolList[i] == true)
                {
                    rtnList.Add(lines[i]);
                }
            }

            return rtnList;
        }

        private List<Line> SelectLines(List<Point3d> areaCents, int maxBranch, List<Line> lines, int seed)
        {
            var rtnList = new List<Line>();
            RhinoWrapper.SortList(ref areaCents);

            for (int i = 0; i < areaCents.Count; i++)
            {
                if (lines.Count == 0) break;
                var connectIdxs =  connectedLineIdx(areaCents[i], lines);

                //つながっているＬｉｎｅがない。
                if (connectIdxs.Count == 0)
                {
                    continue;
                }

                if (connectIdxs.Count == 1)
                {
                    rtnList.Add(lines[connectIdxs[0]]);
                    lines.RemoveAt(connectIdxs[0]);
                    continue;
                }


                var rand = new Random(seed);
                int randVal = rand.Next(1, connectIdxs.Count);
                if (randVal > maxBranch)
                {
                    randVal = maxBranch;
                }

                //important inoredr to doesnt cause error
                if (randVal == connectIdxs.Count)
                {
                    randVal--;
                }

                for (int j = 0; j < randVal; j++)
                {
                    int idx = connectIdxs[j];
                    rtnList.Add(lines[idx]);
                }

                for (int k = 0; k < randVal; k++)
                {
                    int idx = connectIdxs[k];
                    lines.RemoveAt(idx);
                }
            }

            return rtnList;
        }

        List<int> connectedLineIdx(Point3d pt, List<Line> lines)
        {
            var rtnList = new List<int>();
            var buff = new List<int>();

            for (int i = 0; i < lines.Count; i++)
            {

                if (lines[i].From == pt || lines[i].To == pt)
                {
                    buff.Add(i);
                }
            }

            var buffArr = buff.Distinct().ToArray();
            rtnList.AddRange(buffArr);

            return rtnList;
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("e83591c7-abaf-4153-82bb-97f7255a6aa0"); }
        }
    }
}