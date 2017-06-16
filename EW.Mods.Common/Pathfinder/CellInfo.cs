using System;
namespace EW.Mods.Common.Pathfinder
{
    /// <summary>
    /// 
    /// </summary>
    public enum CellStatus
    {
        Unvisited,
        Open,
        Closed,
    }
    public struct CellInfo
    {
        /// <summary>
        /// ���������˽ڵ�����ķ���
        /// </summary>
        public readonly int CostSoFar;

        /// <summary>
        /// ���ƽڵ�������ǵ�Ŀ���ж�Զ
        /// </summary>
        public readonly int EstimatedTotal;

        /// <summary>
        /// �������·����ǰһ���ڵ�
        /// </summary>
        public readonly CPos PreviousPos;

        public readonly CellStatus Status;

        public CellInfo(int costSoFar,int estimatedTotal,CPos previousPos,CellStatus status)
        {
            this.CostSoFar = costSoFar;
            this.EstimatedTotal = estimatedTotal;
            this.PreviousPos = previousPos;
            this.Status = status;
        }
    }
}