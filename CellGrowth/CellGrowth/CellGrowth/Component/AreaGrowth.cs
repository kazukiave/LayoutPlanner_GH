using System;
using System.Collections.Generic;

using Grasshopper.Kernel.Data;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper;

using Rhino.Geometry;

using System.IO;
using System.Linq;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace CellGrowth
{
    public class AreaGrowth : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public AreaGrowth()
          : base("AreaGrowth", "Nickname",
              "Description",
              "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("ShapedGrid", "", "", GH_ParamAccess.list);
            pManager.AddIntegerParameter("gridSize", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("targetArea", "", "", GH_ParamAccess.list);
            pManager.AddIntegerParameter("tolerance", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("AreaPts", "", "", GH_ParamAccess.tree);
            pManager.AddPointParameter("AreaCenters", "", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> others = new List<Point3d>();
            int gridSize = 0;
            List<int> targetArea = new List<int>();
            int tolerance = 0;

            if (!DA.GetDataList(0, others)) return;
            if (!DA.GetData(1, ref gridSize)) return;
            if (!DA.GetDataList(2, targetArea)) return;
            if (!DA.GetData(3, ref tolerance)) return;
            var rtnTree = new DataTree<Point3d>();
            double hypotenuse = Math.Sqrt((gridSize * gridSize) * 2);

            for (int i = 0; i < targetArea.Count; i++)
            {
                SortList(ref others);
                if (others.Count == 0) break;
                var stPts = new List<Point3d>() { others[0] };
                GrowthMethod(stPts, ref others, ref rtnTree, hypotenuse, gridSize, targetArea[i], tolerance, i);
            }

            var AreaCenters = new List<Point3d>();
            for (int i = 0; i < rtnTree.Paths.Count; i++)
            {
                var path = rtnTree.Paths[i];
                var center = new Vector3d();
                foreach (Point3d pt in rtnTree.Branch(path))
                {
                    center += new Vector3d(pt);
                }
                center /= rtnTree.Branch(path).Count;
                AreaCenters.Add(new Point3d(center));
            }


            DA.SetDataTree(0, rtnTree);
            DA.SetDataList(1, AreaCenters);
        }

       
        void GrowthMethod(List<Point3d> stPts,ref List<Point3d> others, ref DataTree<Point3d> rtnTree
            , double distance, int gridSize, int targetArea, int tolerance, int iterat )
        {
          
            var stPtsBuff = new List<Point3d>();

            //start Pts それぞれに処理していき、
            //結果としてothersを減らすのとstPtsを変更していく
            for (int i = 0; i < stPts.Count; i++)
            {
                //範囲内のptsをothersから取り出す
                var RangePt = GetPtsInRange(stPts[i], others, distance + 0.1);
                PtsRemovePts(RangePt, ref others);
                stPtsBuff.AddRange(RangePt);
                rtnTree.AddRange(RangePt, new GH_Path(iterat));
            }
           
            //面積でなくただたんに隣合うポイントをつなげたいとき。
            if (targetArea == 0 && others.Count != 0 && stPtsBuff.Count != 0)
            {
                GrowthMethod(stPtsBuff, ref others, ref rtnTree
            , distance, gridSize, targetArea, tolerance, iterat);
                return;
            }
            else if (targetArea == 0 && others.Count == 0 && stPtsBuff.Count == 0)
            {
                return;
            }
            

            double area = AreaCalculate(rtnTree.Branch(new GH_Path(iterat)).Count, gridSize);
            if (Math.Abs(area - targetArea) < tolerance || area > targetArea ||
                others.Count == 0 || stPtsBuff.Count == 0)
            {
                return;
            }
            else
            {
                GrowthMethod( stPtsBuff, ref others, ref rtnTree
            , distance, gridSize, targetArea, tolerance, iterat);
            }
        }

        void SortList(ref List<Point3d> list)
        {
            var arr = list.ToArray();
            var xArr = arr.Select(pt => pt.X).ToArray();
            var yArr = arr.Select(pt => pt.Y).ToArray();

            System.Array.Sort(arr, xArr);
            System.Array.Sort(arr, yArr);

            list.Clear();
            list.AddRange(arr);
        }

        List<Point3d> GetPtsInRange(Point3d stPt, List<Point3d> others, double distance)
        {
            var rtnList = new List<Point3d>();
            var rPt = others.Where(other => stPt.DistanceTo(other) < distance);
            rtnList.AddRange(rPt.ToArray());
            return rtnList;
        }

        double AreaCalculate(int gridNum, int gridSize)
        {
            int areaSize = gridSize * gridSize;
            return gridNum * areaSize;
        }

        void ToAreaMeter(ref double mm)
        {
            mm = mm / (1000 * 1000);
        }

        void CullDup(ref List<Point3d> pts)
        {
            var rtnTree = new List<Point3d>();

            foreach (var pt in pts)
            {
                if (!rtnTree.Contains(pt))
                {
                    rtnTree.Add(pt);
                }
            }

            pts = rtnTree;
        }

        void PtsRemovePts(List<Point3d> removePt, ref List<Point3d> targetPts)
        {

            var rtnTree = new List<Point3d>();

            foreach (Point3d target in targetPts)
            {
                if (removePt.Contains(target) == false)
                {
                    rtnTree.Add(target);
                }
            }
            targetPts = rtnTree;
            return;
        }

        List<Point3d> PtsContainsPts(List<Point3d> targets, List<Point3d> others)
        {
            var rtnArr = targets.Where(target => others.Contains(target) == true).ToArray();
            var rtnTree = new List<Point3d>();
            rtnTree.AddRange(rtnArr);
            return rtnTree;
        }



        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8def97f7-a29b-46b2-b064-10531f46f33b"); }
        }
    }
}
