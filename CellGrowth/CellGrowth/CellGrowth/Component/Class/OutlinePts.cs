using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace CellGrowth
{
    public class OutlinePts
    {
        public List<Vector3d> left { get; set; }
        public List<Vector3d> right { get; set; }
        public List<Vector3d> up { get; set; }
        public List<Vector3d> down { get; set; }

        public List<Vector3d> all { get; set; }

        public OutlinePts()
        {
          
        }
        public List<Vector3d> GetAll()
        {
            var rtnList = new List<Vector3d>();
            rtnList.AddRange(left);
            rtnList.AddRange(right);
            rtnList.AddRange(up);
            rtnList.AddRange(down);

            return rtnList;
        }

        public void ClearMaxCountList()
        {
            if (left.Count == GetMaxCount())
            {
                left.Clear();
                return;
            }

            if (right.Count == GetMaxCount())
            {
                right.Clear();
                return;
            }

            if (up.Count == GetMaxCount())
            {
                up.Clear();
                return;
            }

            if (down.Count == GetMaxCount())
            {
                down.Clear();
                return;
            }
        }
        public int GetMaxCount()
        {
            List<int> counts = new List<int>();

            counts.Add(left.Count);
            counts.Add(right.Count);
            counts.Add(up.Count);
            counts.Add(down.Count);

            return counts.Max(); 
        }

        public List<Vector3d> GetMaxCountList()
        {
            List<int> counts = new List<int>();

            counts.Add(left.Count);
            counts.Add(right.Count);
            counts.Add(up.Count);
            counts.Add(down.Count);

            int maxCount = counts.Max();
            if (left.Count == maxCount)
            {
                return left;
            }
            if (right.Count == maxCount)
            {
                return right;
            }
            if (up.Count == maxCount)
            {
                return up;
            }
            if (down.Count == maxCount)
            {
                return down;
            }

            return new List<Vector3d>();
        }
    }
}
