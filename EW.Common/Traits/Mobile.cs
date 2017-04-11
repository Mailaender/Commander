using System;


namespace EW.Mods.Common.Traits
{

    /// <summary>
    /// 
    /// </summary>
    public class MobileInfo:UpgradableTraitInfo,IMoveInfo,IPositionableInfo,IOccupySapceInfo,IFacingInfo,
        UsesInit<FacingInit>, UsesInit<LocationInit>, UsesInit<SubCellInit>
    {
        /// <summary>
        /// ������Ϣ
        /// </summary>
        public class TerrainInfo
        {
            /// <summary>
            /// ����ͨ��
            /// </summary>
            public static readonly TerrainInfo Impassable = new TerrainInfo();

            public readonly int Cost;
            public readonly int Speed;

            public TerrainInfo()
            {
                Cost = int.MaxValue;
                Speed = 0;
            }

            public TerrainInfo(int speed,int cost)
            {
                Speed = speed;
                Cost = cost;
            }
        }
    }

    public class Mobile:UpgradableTrait<>
    {
    }
}