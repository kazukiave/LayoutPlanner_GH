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


namespace CellGrowth
{
    public class RectGrowth : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GrowthRect2 class.
        /// </summary>
        public RectGrowth()
          : base("RectGrowth", "Nickname",
              "Description",
              "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddVectorParameter("rectPts", "", "", GH_ParamAccess.tree);
            pManager.AddVectorParameter("spacePts", "", "EmptyGridPts", GH_ParamAccess.list);
            pManager.AddIntegerParameter("gridSize", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("targetAreaSize", "", "", GH_ParamAccess.list);
            pManager.AddIntegerParameter("iteration", "", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("mingridNum", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddVectorParameter("rectPts", "", "", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var rectPts = new DataTree<Vector3d>();
            var spacePts = new List<Vector3d>();
            int gridSize = 0;
            var targetAreaSize = new List<int>();
            int iteration = 0;
            int minGridNum = 0;

            Rhino.RhinoApp.WriteLine("debug0");
            //Convert GH_DataTree to DataTree
            var tree = new GH_Structure<GH_Vector>();
            if (DA.GetDataTree(0, out tree))
            {
                for (int i = 0; i < tree.PathCount; i++)
                {
                    var path = tree.Paths[i];
                    foreach (var vec in tree[path])
                    {
                        rectPts.Add(vec.Value, path);
                    }
                }
            }
            if (!DA.GetDataList(1, spacePts)) return;
            if (!DA.GetData(2, ref gridSize)) return;
            if (!DA.GetDataList(3, targetAreaSize)) return;
            DA.GetData(4, ref iteration);
            if(!DA.GetData(5, ref minGridNum)) return ;

            var rtnTree = new DataTree<Vector3d>();
            rtnTree.MergeTree(rectPts);


            int num_rect = rectPts.BranchCount;
            int count = 0;
            //loop
            //  while (canGrowthTree.AllData().Contains(true))
            while (count < iteration)
            {
                count++;

                var sortedDictionary = new SortedDictionary<double, GH_Path>();
                for (int i = 0; i < num_rect; i++)
                {
                    var path = rectPts.Paths[i];
                    var rect = rectPts.Branch(path);

                    var diff = (targetAreaSize[i] - AreaCalculate(rect.Count, gridSize));
                    sortedDictionary.Add(diff, path);
                }

                var sortedList = sortedDictionary.Values.ToList();
                sortedList.Reverse();

                int isEnd = 0;
                for (int i = 0; i < num_rect; i++)
                {
                    //このパスを面積の差が多きやつからにすればそうなる。
                    var path = sortedList[i];
                    var rect = rectPts.Branch(path);
                    var outlinePts = new OutlinePts
                    {
                        left = GetNextLeftLine(rect, spacePts, gridSize),
                        right = GetNextRightLine(rect, spacePts, gridSize),
                        up = GetNextUpLine(rect, spacePts, gridSize),
                        down = GetNextDownLine(rect, spacePts, gridSize)
                    };

                    if (outlinePts.GetAll().Count == 0 || outlinePts.GetMaxCountList().Count < minGridNum) continue;

                    isEnd++;

                    List<Vector3d> nextPos = outlinePts.GetMaxCountList();
                    rectPts.AddRange(nextPos, path);
                    ListRemoveList(ref spacePts, nextPos);
                }
                if (isEnd == 0) break;
            }
            DA.SetDataTree(0, rectPts);
        }

         private double AreaCalculate(int gridNum, int gridSize)
        {
            int areaSize = gridSize * gridSize;
            return gridNum * areaSize;
        }

        private void ListRemoveList(ref List<Vector3d> source, List<Vector3d> delData)
        {
            for (int i = 0; i < delData.Count; i++)
            {
                for (int j = 0; j < source.Count; j++)
                {
                    if (delData[i] == source[j])
                    {
                        source.RemoveAt(j);
                    }
                }
            }
        }

        private bool ListContainsList(List<Vector3d> child, List<Vector3d> parent)
        {
            var rtnVal = true;

            for (int i = 0; i < child.Count; i++)
            {
                if (!parent.Contains(child[i]))
                {
                    return false;
                }
            }

            return rtnVal;
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

        double Hypotenuse(int gridSize)
        {
        return Math.Sqrt((gridSize * gridSize) * 2);
        }

     
        List<Point3d> VecToPt(List<Vector3d> vec)
        {
            var rtnList = new List<Point3d>();

            var rtnArr = vec.Select(v => new Point3d(v)).ToArray();
            rtnList.AddRange(rtnArr);
            return rtnList;
        }

        List<Vector3d> PtToVec(List<Point3d> pts)
        {
            var rtnList = new List<Vector3d>();

            var rtnArr = pts.Select(pt => new Vector3d(pt)).ToArray();
            rtnList.AddRange(rtnArr);
            return rtnList;
        }


        private List<Vector3d> GetNextLeftLine(List<Vector3d> outLine, List<Vector3d> spacePts, int gridSize)
        {
            var rtnList = new List<Vector3d>();

            Bbox _bbox = new Bbox(outLine);
            for (int i = 0; i < outLine.Count; i++)
            {
                if (outLine[i].X == _bbox.Min.X)
                {

                    var nextPos = new Vector3d(outLine[i].X - gridSize
                                        , outLine[i].Y
                                        , 0);
                    if(spacePts.Contains(nextPos))
                    rtnList.Add(nextPos);
                }
            }

            //二つに分かれたときは数の多いほうを返す。
            var rtnTree = new DataTree<Point3d>();
            double hypotenuse = Math.Sqrt((gridSize * gridSize) * 2);
            var rtnListPts = VecToPt(rtnList);

            int iterat = 0;
            while (rtnListPts.Count > 0 && iterat < 1000)
            {
                SortList(ref rtnListPts);
                var stPts = new List<Point3d>() { rtnListPts[0] };
                ConnectPts(stPts, ref rtnListPts, ref rtnTree, hypotenuse, 10, 1, iterat);
                iterat++;
            }

            if(rtnTree.BranchCount == 1)
            {
                return rtnList;
            }

            if (rtnTree.BranchCount > 1)
            {
                GH_Path maxPath = new GH_Path(0);
                int maxCount = 0;
                for (int i = 0; i < rtnTree.BranchCount; i++)
                {
                    var path = rtnTree.Paths[i];
                    if (rtnTree.Branch(path).Count > maxCount)
                    {
                        maxCount = rtnTree.Branch(path).Count;
                        maxPath = path;
                    }
                }
                return PtToVec(rtnTree.Branch(maxPath));
            }

            return rtnList;
        }

        private List<Vector3d> GetNextRightLine(List<Vector3d> outLine, List<Vector3d> spacePts, int gridSize)
        {
            var rtnList = new List<Vector3d>();

            Bbox _bbox = new Bbox(outLine);
            for (int i = 0; i < outLine.Count; i++)
            {
                if (outLine[i].X == _bbox.Max.X)
                {
                    var nextPos = new Vector3d(outLine[i].X + gridSize
                                     , outLine[i].Y
                                     , 0);
                    if (spacePts.Contains(nextPos))
                        rtnList.Add(nextPos);
                }
            }

            //二つに分かれたときは数の多いほうを返す。
            var rtnTree = new DataTree<Point3d>();
            double hypotenuse = Math.Sqrt((gridSize * gridSize) * 2);
            var rtnListPts = VecToPt(rtnList);

            int iterat = 0;
            while (rtnListPts.Count > 0 && iterat < 1000)
            {
                SortList(ref rtnListPts);
                var stPts = new List<Point3d>() { rtnListPts[0] };
                ConnectPts(stPts, ref rtnListPts, ref rtnTree, hypotenuse, 10, 1, iterat);
                iterat++;
            }

            if (rtnTree.BranchCount == 1)
            {
                return rtnList;
            }

            if (rtnTree.BranchCount > 1)
            {
                GH_Path maxPath = new GH_Path(0);
                int maxCount = 0;
                for (int i = 0; i < rtnTree.BranchCount; i++)
                {
                    var path = rtnTree.Paths[i];
                    if (rtnTree.Branch(path).Count > maxCount)
                    {
                        maxCount = rtnTree.Branch(path).Count;
                        maxPath = path;
                    }
                }
                return PtToVec(rtnTree.Branch(maxPath));
            }
           

            return rtnList;
        }

        private List<Vector3d> GetNextUpLine(List<Vector3d> outLine, List<Vector3d> spacePts, int gridSize)
        {
            var rtnList = new List<Vector3d>();

            Bbox _bbox = new Bbox(outLine);
            for (int i = 0; i < outLine.Count; i++)
            {
                if (outLine[i].Y == _bbox.Max.Y)
                {
                    var nextPos = new Vector3d(outLine[i].X
                                      , outLine[i].Y + gridSize
                                      , 0);

                    if (spacePts.Contains(nextPos))
                        rtnList.Add(nextPos);
                }
            }
            //二つに分かれたときは数の多いほうを返す。
            var rtnTree = new DataTree<Point3d>();
            double hypotenuse = Math.Sqrt((gridSize * gridSize) * 2);
            var rtnListPts = VecToPt(rtnList);

            int iterat = 0;
            while (rtnListPts.Count > 0 && iterat < 1000)
            {
                SortList(ref rtnListPts);
                var stPts = new List<Point3d>() { rtnListPts[0] };
                ConnectPts(stPts, ref rtnListPts, ref rtnTree, hypotenuse, 10, 1, iterat);
                iterat++;
            }

            if (rtnTree.BranchCount == 1)
            {
                return rtnList;
            }

            if (rtnTree.BranchCount > 1)
            {
                GH_Path maxPath = new GH_Path(0);
                int maxCount = 0;
                for (int i = 0; i < rtnTree.BranchCount; i++)
                {
                    var path = rtnTree.Paths[i];
                    if (rtnTree.Branch(path).Count > maxCount)
                    {
                        maxCount = rtnTree.Branch(path).Count;
                        maxPath = path;
                    }
                }
                return PtToVec(rtnTree.Branch(maxPath));
            }

            return rtnList;
        }
        private List<Vector3d> GetNextDownLine(List<Vector3d> outLine, List<Vector3d> spacePts, int gridSize)
        {
            var rtnList = new List<Vector3d>();

            Bbox _bbox = new Bbox(outLine);
            for (int i = 0; i < outLine.Count; i++)
            {
                if (outLine[i].Y == _bbox.Min.Y)
                {
                    var nextPos = new Vector3d(outLine[i].X
                                     , outLine[i].Y - gridSize
                                     , 0);
                    if (spacePts.Contains(nextPos))
                        rtnList.Add(nextPos);
                }
            }

            //二つに分かれたときは数の多いほうを返す。
            var rtnTree = new DataTree<Point3d>();
            double hypotenuse = Math.Sqrt((gridSize * gridSize) * 2);
            var rtnListPts = VecToPt(rtnList);

            int iterat = 0;
            while (rtnListPts.Count > 0 && iterat < 1000)
            {
                SortList(ref rtnListPts);
                var stPts = new List<Point3d>() { rtnListPts[0] };
                ConnectPts(stPts, ref rtnListPts, ref rtnTree, hypotenuse, 10, 1, iterat);
                iterat++;
            }

            if (rtnTree.BranchCount == 1)
            {
                return rtnList;
            }

            if (rtnTree.BranchCount > 1)
            {
                GH_Path maxPath = new GH_Path(0);
                int maxCount = 0;
                for (int i = 0; i < rtnTree.BranchCount; i++)
                {
                    var path = rtnTree.Paths[i];
                    if (rtnTree.Branch(path).Count > maxCount)
                    {
                        maxCount = rtnTree.Branch(path).Count;
                        maxPath = path;
                    }
                }
                return PtToVec(rtnTree.Branch(maxPath));
            }
            return rtnList;
        }



        void ConnectPts(List<Point3d> stPts, ref List<Point3d> others, ref DataTree<Point3d> rtnTree
          , double distance, int gridSize, int tolerance, int iterat)
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
            if (others.Count != 0 && stPtsBuff.Count != 0)
            {
                ConnectPts(stPtsBuff, ref others, ref rtnTree
                  , distance, gridSize, tolerance, iterat);
                return;
            }
            else if (others.Count == 0 && stPtsBuff.Count == 0)
            {
                return;
            }

            if (others.Count == 0 || stPtsBuff.Count == 0)
            {
                return;
            }
            else
            {
                ConnectPts(stPtsBuff, ref others, ref rtnTree
                  , distance, gridSize, tolerance, iterat);
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
            get { return new Guid("bcded0b0-ba9f-486d-b1c2-33e9c604aa52"); }
        }
    }
}