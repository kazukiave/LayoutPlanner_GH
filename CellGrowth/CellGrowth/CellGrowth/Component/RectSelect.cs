using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper;
using Grasshopper.Kernel.Geometry;

using Rhino.Geometry;
using System.Linq;
using myRhinoWrapper;

namespace CellGrowth.Component
{
    public class RectSelect : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the RectSelect class.
        /// </summary>
        public RectSelect()
          : base("RectSelect", "Nickname",
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
            pManager.AddPointParameter("GridPts", "", "", GH_ParamAccess.list);
            pManager.AddIntegerParameter("GridSize", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("RectPts", "", "", GH_ParamAccess.tree);
            pManager.AddPointParameter("otherPts", "", "", GH_ParamAccess.tree);
            pManager.AddRectangleParameter("Rect", "", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var AreaCenters = new List<Point3d>();
            var gridPts = new List<Point3d>();
            int gridSize = 0;

            if (!DA.GetDataList(0, AreaCenters)) return;
            if (!DA.GetDataList(1, gridPts)) return;
            if (!DA.GetData(2, ref gridSize)) return;

            var dists = RhinoWrapper.DistNearPt(AreaCenters);
            var intervals = MakeInterval(dists, gridSize);
            var rects = new List<Rectangle3d>();
            for (int i = 0; i < AreaCenters.Count; i++)
            {
                var plane = new Rhino.Geometry.Plane(AreaCenters[i], Vector3d.ZAxis);
                rects.Add(new Rectangle3d(plane, intervals[i], intervals[i]));
            }

            var rectPts = SelRectPts(gridPts, rects);
            var otherPts = gridPts.Where(pt => rectPts.AllData().Contains(pt) == false).ToList();

            DA.SetDataTree(0, rectPts);
            DA.SetDataList(1, otherPts);
            DA.SetDataList(2, rects);
        }

        private List<Interval> MakeInterval(List<double> dists, int gridSize)
        {
            var rtnList = new List<Interval>();

            for (int i = 0; i < dists.Count; i++)
            {
                var interValue = (dists[i] / 2) - gridSize;
                var inter = new Interval(interValue * -1, interValue);
                rtnList.Add(inter);
            }

            return rtnList;
        }

        private DataTree<Point3d> SelRectPts(List<Point3d> gridPts, List<Rectangle3d> excArea)
        {
            var rectPts = new DataTree<Point3d>();


            for (int i = 0; i < gridPts.Count; i++)
            {
                int count = 0;
                
                for (int j = 0; j < excArea.Count; j++)
                {
                    if (IsInside(gridPts[i], excArea[j].ToPolyline()))
                    {
                        count++;
                        rectPts.Add(gridPts[i], new GH_Path(j));
                    }
                }
            }

           return rectPts;
        }
        private bool IsInside(Point3d pt, Polyline crv)
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
            get { return new Guid("1c83542b-72ed-40c3-b762-f5b71a994197"); }
        }
    }
}