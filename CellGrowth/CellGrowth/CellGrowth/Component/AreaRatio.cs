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
    public class AreaRatio : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the AreaRatio class.
        /// </summary>
        public AreaRatio()
          : base("AreaRatio", "Nickname",
              "Description",
              "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("TargetCurve", "", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("RationMin", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("RoomNum", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Seed", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Ratio", "", "", GH_ParamAccess.list);
            pManager.AddNumberParameter("Area", "", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Curve targetCrv = null;
            double ratioMin = 0;
            int roomNum = 0 ;
            int seed = 0;

            if (!DA.GetData(0, ref targetCrv)) return;
            if (!DA.GetData(1, ref ratioMin)) return;
            if (!DA.GetData(2, ref roomNum)) return;
            if (!DA.GetData(3, ref seed)) return;


            var rtnList = new List<double>();
            var ratioList = new List<double>();
          
            double targetArea = Rhino.Geometry.AreaMassProperties.Compute(targetCrv, 0.1).Area;
            double rationMax = 1f / (float)(roomNum);
            
            for (int i = 0; i < roomNum - 1; i++)
            {
                var rand = new Random(seed * i);
                var randVal = rand.NextDouble() * (rationMax - ratioMin) + ratioMin;
                ratioList.Add(randVal);
            }
            ratioList.Add(1 - ratioList.Sum());

            var areaArr = ratioList.Select(ratio => ratio * targetArea).ToArray();
            rtnList.AddRange(areaArr);
            rtnList.Jitter();

            DA.SetDataList(0, ratioList);
            DA.SetDataList(1, rtnList);
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
            get { return new Guid("7cd50e2a-34cb-48ff-9e34-c4ae6f3bc6a8"); }
        }
    }
}