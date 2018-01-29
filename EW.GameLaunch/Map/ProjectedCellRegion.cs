using System;
using System.Collections;
using System.Collections.Generic;
namespace EW
{
    /// <summary>
    /// Ͷ�䷶Χ��Ԫ��
    /// </summary>
    public class ProjectedCellRegion:IEnumerable<PPos>
    {

        //Corner of the region
        public readonly PPos TopLeft;
        public readonly PPos BottomRight;

        /// <summary>
        /// ��ͶӰ��ͼ�����ڰ��������е�Ԫ��,������Ӧ�ñ�Ͷ���ڴ�����
        /// </summary>
        readonly MPos mapTopLef;
        readonly MPos mapBottomRight;

        public ProjectedCellRegion(Map map,PPos topLeft,PPos bottomRight)
        {
            TopLeft = topLeft;
            BottomRight = bottomRight;

            //The projection from MPos->PPos cannot produce a larger V coordinate
            //so the top edge of the MPos region is the same as the PPos region.
            //(in fact the cells are identical if height ==0)
            //MPos -> PPos ��ͶӰ���ܲ����ϴ��V ���꣬���MPos�����ڵĶ����� PPos ��ͬ(��ʵ�ϣ����height == 0,��Ԫ����һ����)
            mapTopLef = (MPos)topLeft;

            var maxHeight = map.Grid.MaximumTerrainHeight;
            var heightOffset = map.Grid.Type == MapGridT.RectangularIsometric ? maxHeight : maxHeight / 2;

            //Use the map Height data array to clamp the bottom coordinate so it doesn't overflow the map
            //ʹ�õ�ͼ�߶��������������Ƶײ����꣬ʹ�䲻�������ͼ
            mapBottomRight = map.Height.Clamp(new MPos(bottomRight.U, bottomRight.V + heightOffset));
        }

        public ProjectedCellRegionEnumerator GetEnumerator()
        {
            return new ProjectedCellRegionEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<PPos> IEnumerable<PPos>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// ��ͼ����ϵ�а�������ͶӰ�ڸ������ڵ����е�Ԫ����Ϣ
        /// Ϊ��������ܣ�������֤������ͼ��Ԫ���Ƿ�����Ӧ�ڵ�ǰ������ͶӰ
        /// </summary>
        public MapCoordsRegion CandidateMapCoords { get { return new MapCoordsRegion(mapTopLef, mapBottomRight); } }


        /// <summary>
        /// 
        /// </summary>
        public sealed class ProjectedCellRegionEnumerator : IEnumerator<PPos>
        {
            readonly ProjectedCellRegion r;

            int u, v;

            PPos current;

            public ProjectedCellRegionEnumerator(ProjectedCellRegion region)
            {
                r = region;
                Reset();
            }

            public bool MoveNext()
            {
                u += 1;
                //��������
                if (u > r.BottomRight.U)
                {
                    v += 1;
                    u = r.TopLeft.U;
                    //��� �����
                    if (v > r.BottomRight.V)
                        return false;
                }
                current = new PPos(u, v);
                return true;
            }
            public void Reset()
            {
                u = r.TopLeft.U - 1;
                v = r.TopLeft.V;
            }

            public PPos Current { get { return current; } }

            object IEnumerator.Current { get { return Current; } }

            public void Dispose() { }
        }
    }
}