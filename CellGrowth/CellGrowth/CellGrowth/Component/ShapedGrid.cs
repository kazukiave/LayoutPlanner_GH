using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Linq;
using myRhinoWrapper;

namespace Component
{
    public class ShapedGrid : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ShapedGrid class.
        /// </summary>
        public ShapedGrid()
          : base("ShapedGrid", "Nickname",
              "Description",
              "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("GridSize","", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("X_Ex", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Y_Ex", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("OffsetValue", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("OffsetValue2", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("MaxReduceNum", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Seed", "", "", GH_ParamAccess.item);
            pManager.AddPointParameter("Origin", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("ShapedGrid", "", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("Shape", "", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("originalShape", "", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("offsetShape1", "", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("offsetShape2", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int gridSize = 0;
            int x_Ex = 0;
            int y_Ex = 0;
            int offsetVal = 0;
            int offsetVal2 = 0;
            int reduceNum = 0;
            int seed = 0;
            var center = new Rhino.Geometry.Point3d();

            if (!DA.GetData(0, ref gridSize)) return;
            if (!DA.GetData(1, ref x_Ex)) return;
            if (!DA.GetData(2, ref y_Ex)) return;
            if (!DA.GetData(3, ref offsetVal)) return;
            if (!DA.GetData(4, ref offsetVal2)) return;
            if (!DA.GetData(5, ref reduceNum)) return;
            if (!DA.GetData(6, ref seed)) return;
            if (!DA.GetData(7, ref center)) return;

         
           
            var grid = RhinoWrapper.MakeGrid(x_Ex, y_Ex, gridSize);
            var rectMain = RhinoWrapper.MakeRect(center, x_Ex, y_Ex, gridSize);
            var rectMainCrv = rectMain.ToPolyline().ToPolylineCurve();
            var corners = new Rhino.Geometry.Point3d[4];
            for (int i = 0; i < 4; i++)
            {
                corners[i] = rectMain.Corner(i);
            }

            var rectSub = RhinoWrapper.MakeRect(center, x_Ex, y_Ex, gridSize, offsetVal);
            var rectSub2 = RhinoWrapper.MakeRect(center, x_Ex, y_Ex, gridSize, offsetVal2);
            var populate = RhinoWrapper.RandomPt(rectSub, reduceNum);
            var populateInRange = populate.Where(pt => RhinoWrapper.IsInside(pt, rectSub2.ToPolyline()) == false).ToArray();
            var randPts = new List<Point3d>(populateInRange);

            var planeXY = new Rhino.Geometry.Plane(Point3d.Origin, Vector3d.ZAxis);
            var reduceRects = new List<Rhino.Geometry.PolylineCurve>();

            for (int i = 0; i < randPts.Count; i++)
            {
                var pc = new Rhino.Geometry.PointCloud(corners);
                int closestIdx = pc.ClosestPoint(randPts[i]);
                var reduceRect = new Rectangle3d(planeXY, randPts[i], corners[closestIdx]);
                var polyCrv = reduceRect.ToPolyline().ToPolylineCurve();
                reduceRects.Add(polyCrv);
            }
            
            var shape = Curve.CreateBooleanDifference(rectMainCrv, reduceRects, 0.1);
            if (shape.Length > 0)
            {
                var polyShape = new Rhino.Geometry.Polyline();
                shape[0].TryGetPolyline(out polyShape);
                var rtnArr = grid.Where(pt => RhinoWrapper.IsInside(pt, polyShape)).ToArray();

                DA.SetDataList(0, new List<Point3d>(rtnArr));
                DA.SetData(1, polyShape);
                DA.SetData(2, rectMain.ToPolyline());
                DA.SetData(3, rectSub.ToPolyline());
                DA.SetData(4, rectSub2.ToPolyline());
            }
            else
            {
                Rhino.RhinoApp.WriteLine("no shape");
                DA.SetDataList(0, grid);
                DA.SetData(1, rectMain.ToPolyline());
            }
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
            get { return new Guid("4490ca5b-8076-4c00-9985-b682677e5e27"); }
        }
    }
}