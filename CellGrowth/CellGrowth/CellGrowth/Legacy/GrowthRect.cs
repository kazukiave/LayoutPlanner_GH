using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;

using Rhino.DocObjects;
using Rhino.Collections;
using GH_IO;
using GH_IO.Serialization;
using Grasshopper;

namespace CellGrowth
{
    abstract class GrowthRect : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GrowthRect class.
        /// </summary>
        public GrowthRect()
          : base("GrowthRect", "aaa",
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
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddVectorParameter("rectPts", "test", "", GH_ParamAccess.tree);
            pManager.AddVectorParameter("spacePts", "test", "", GH_ParamAccess.list);
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

            Rhino.RhinoApp.WriteLine("debug1");
            Rhino.RhinoApp.WriteLine("gridSize =" + gridSize);

            var rtnTree = new DataTree<Vector3d>();
            rtnTree.MergeTree(rectPts);
           
            Rhino.RhinoApp.WriteLine("debug2");

            int num_rect = rectPts.BranchCount;
            //Iteration each rectangler
            //canGrow[x] = rectangle num
            //canGrow[][0-3] = left, up, right, down の方向に進めるかどうか
           


            int count = 0;
            //loop
          //  while (canGrowthTree.AllData().Contains(true))
            while(count < iteration)
            {
                DataTree<bool> canGrowthTree = MakeCanGrowthTree(num_rect); //rect num*4 true tree
                count++;

                var outlinePtsList = new List<OutlinePts>();
                //outlinePtsListにはデータツリー状にそれぞれの四角形の4方向の次のポイント入っている。
                for (int i = 0; i < num_rect; i++)
                {
                    var path = rectPts.Paths[i];
                    var rect = rectPts.Branch(path);
                    outlinePtsList.Add(GetNextOutlinePts(rect, gridSize, canGrowthTree.Branch(path)));
                }


                //すべてのレクタングルと目標面積の差をとって差が大きい順のIndex（パス）を出す。
                //このとき直接レクタングルを並びかえるとどのエージェントに対するレクタングルかわからなくなる。
                List<int> curAreaSize = AreaSize(rectPts, gridSize);
                List<int> sortIndexs = AreaComparSort(curAreaSize, targetAreaSize);
             

                //それぞれの四角形をExpandしていくために
                //レクタングルの四側にある点で一番数が多くて壁から遠いほうにExpandし、一番面積が増えるように操作する。
                //他の点がない　かつ　SpacePts内に点がある（壁ではない）
                for (int i = 0; i < num_rect; i++)
                {
                    GH_Path path = rectPts.Paths[i];
                    List<Vector3d> rect = rectPts.Branch(path); //一番面積の差が開いていたRectangle
                    OutlinePts rectsOutline = outlinePtsList[sortIndexs[i]];//で、そのレクタングルのアウトライン

                    for (int j = 0; j < 4; j++)
                    {
                        List<Vector3d> nextPos = rectsOutline.GetMaxCountList();
                        if (ListContainsList(nextPos, spacePts))
                        {
                            rectPts.AddRange(nextPos, new GH_Path(sortIndexs[i]));
                            ListRemoveList(ref spacePts, nextPos);
                            j += 4;
                        }
                        else
                        {
                            rectsOutline.ClearMaxCountList();
                            canGrowthTree[path, j] = false;
                        }
                    }
                }

                if (canGrowthTree.AllData().Contains(true) == false) break;
            }

            DA.SetDataTree(0, rectPts);
            DA.SetDataList(1, spacePts);
      }
        private DataTree<bool> MakeCanGrowthTree(int rect_num)
        {
            var rtnTree = new DataTree<bool>();

            for (int i = 0; i < rect_num; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    rtnTree.Add(true, new GH_Path(i));
                }
            }
            return rtnTree;
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
               if(!parent.Contains(child[i]))
                {
                    return false;
                }
            }

            return rtnVal;
        }

        private OutlinePts GetNextOutlinePts(List<Vector3d> rectPts, int gridSize, List<bool> canGrowth)
        { 
            OutlinePts outlinePts = new OutlinePts();

            if (canGrowth[0] == true)
            {
                outlinePts.left = GetNextLeftLine(rectPts, gridSize);
            }
            else
            {
                outlinePts.left = new List<Vector3d>();
            }

            if (canGrowth[1] == true)
            {
                outlinePts.right = GetNextRightLine(rectPts, gridSize);
            }
            else
            {
                outlinePts.right = new List<Vector3d>();
            }

            if (canGrowth[2])
            {
                outlinePts.up = GetNextUpLine(rectPts, gridSize);
            }
            else
            {
                outlinePts.up = new List<Vector3d>();
            }

            if (canGrowth[3])
            {
                outlinePts.down = GetNextDownLine(rectPts, gridSize);
            }
            else
            {
                outlinePts.down = new List<Vector3d>();
            }

            return outlinePts;
        } 

        public class AreaIndex
        {
            public int Index { get; set; }
           public  float Area { get; set; }
            public AreaIndex()
            {
              
            }
            public AreaIndex(float area, int idx)
            {
                Area = area;
                Index = idx;
            } 
        }

        /// <summary>
        /// target Areaに対して現在の面積が小さい順にパスが組まれる。
        /// </summary>
        /// <param name="curArea"></param>
        /// <param name="targetArea"></param>
        /// <returns></returns>
        private List<int> AreaComparSort(List<int> curArea, List<int> targetArea)
        {
            var rtnList = new List<int>();
          
            List<AreaIndex> areaIndices = new List<AreaIndex>();

            for (int i = 0; i < curArea.Count; i++)
            {
                int diffArea = targetArea[i] - curArea[i];

                areaIndices.Add(new AreaIndex(diffArea, i));
            }

            areaIndices = areaIndices.OrderBy(areaIndex => areaIndex.Area).ToList();
            foreach (var areaIndex in areaIndices)
            {
                rtnList.Add(areaIndex.Index);
            }
            rtnList.Reverse();

            return rtnList;
        }

        /// <summary>
        /// return index
        /// </summary>
        /// <returns></returns>

        private List<int> AreaSize(DataTree<Vector3d> rectPts, int gridSize)
        {
            var rtnList = new List<int>();
            int gridArea = (gridSize * gridSize) / 2;

            for (int i = 0; i < rectPts.BranchCount; i++)
            {
                var path = rectPts.Paths[i];
                int rectArea = rectPts.Branch(path).Count * gridArea;
                rtnList.Add(rectArea);
            }

            return rtnList;
        }

        private List<Vector3d> GetNextBound(List<Vector3d> outLine, int gridSize)
        {
            var rtnList = new List<Vector3d>();

            Bbox _bbox = new Bbox(outLine);
            Vector3d YmaxXmin = new Vector3d(_bbox.Min.X, _bbox.Max.Y, 0);
            Vector3d YminXmax = new Vector3d(_bbox.Max.X, _bbox.Min.Y, 0);

            float diagoLen = gridSize * (float)Math.Sqrt(2);

            for (int i = 0; i < outLine.Count; i++)
            {
                if (outLine[i].X == _bbox.Min.X)
                {
                    rtnList.Add(outLine[i]);
                }
            }

            return rtnList;
        }


        private List<Vector3d> GetNextLeftLine(List<Vector3d> outLine, int gridSize)
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

                    rtnList.Add(nextPos);
                }
            }

            return rtnList;
        }

        private List<Vector3d> GetNextRightLine(List<Vector3d> outLine, int gridSize)
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

                    rtnList.Add(nextPos);
                }
            }

            return rtnList;
        }

        private List<Vector3d> GetNextUpLine(List<Vector3d> outLine, int gridSize)
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

                    rtnList.Add(nextPos);
                }
            }

            return rtnList;
        }
        private List<Vector3d> GetNextDownLine(List<Vector3d> outLine, int gridSize)
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

                    rtnList.Add(nextPos);
                }
            }

            return rtnList;
        }



        /// <summary>
        /// return a rect outlines position List 
        /// </summary>
        /// <param name="rectPts"></param>
        /// <returns></returns>
        private List<Vector3d> GetOutlinePos(List<Vector3d> rectPts)
        {
            var rtnList = new List<Vector3d>();

            Bbox _bbox = new Bbox(rectPts);
            
            for (int i = 0; i < rectPts.Count; i++)
            {
                if (rectPts[i].X == _bbox.Max.X ||
                      rectPts[i].Y == _bbox.Max.Y ||
                         rectPts[i].X == _bbox.Min.X ||
                           rectPts[i].Y == _bbox.Min.Y )
                {
                    rtnList.Add(rectPts[i]);
                }
            }

            return rtnList;
        }

        private List<List<bool>> MakeCanGrowList(int rectCount)
        {
            var rtnList = new List<List<bool>>();

            for (int i = 0; i < rectCount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    rtnList[i][j] = true;
                }
            }

            return rtnList;
        }

        /*
        private Dictionary<string,Vector3d> GetNextPosition()
        { 
            
        }
        */
   
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
            get { return new Guid("8cc43fcb-a194-46f8-8e69-8641b3a9c78e"); }
        }
    }
}