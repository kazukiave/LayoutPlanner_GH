using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Linq;

namespace CellGrowth.Component
{
    public class CurveIntersects : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CurveIntersects class.
        /// </summary>
        public CurveIntersects()
          : base("CurveIntersects", "Nickname",
              "callclate intersect no contain endpt",
              "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("crvs", "", "", GH_ParamAccess.list);
            pManager.AddIntegerParameter("targetIdx", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("interPts", "", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("targetCrv", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var crvs = new List<Curve>();
            var targetIdx = 0; 

            DA.GetDataList(0, crvs);
            DA.GetData(1, ref targetIdx);

            var target = crvs[targetIdx];
            crvs.RemoveAt(targetIdx);

            var crvEnds = AllendPts(crvs);
            var crvInterPts = IntersectCurves(target, crvs);
            var interPts = CullContained(crvInterPts, crvEnds);

            DA.SetDataList(0, interPts);
            DA.SetData(1, crvs[targetIdx]);
        }

        private List<Point3d> CullContained(List<Point3d> baseList, List<Point3d> cullList)
        {
            var rtnList = baseList.Where(pt => cullList.Contains(pt) == false).ToList();

            return rtnList;
        }

        private List<Point3d> IntersectCurves(Curve target, List<Curve> others)
        {
            var rtnList = new List<Point3d>();
            for (int i = 0; i < others.Count; i++)
            {
                var intersects = Rhino.Geometry.Intersect.Intersection.CurveCurve(target, others[i], 1, 1);
                foreach (var interEvent in intersects)
                {
                    if (interEvent.IsPoint == true)
                        rtnList.Add(interEvent.PointA);
                }
            }
            return rtnList;
        }

        private List<Point3d> AllendPts(List<Curve> crvs)
        {
            var endPts = new List<Point3d>();
            foreach (var crv in crvs)
            {
                endPts.Add(crv.PointAtStart);
                endPts.Add(crv.PointAtEnd);
            }

            return endPts;
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
            get { return new Guid("099aa2b9-fc9c-4a55-baa3-41c84d9050d3"); }
        }
    }
}