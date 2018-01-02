using System;
using EW.OpenGLES;
namespace EW
{
    /// <summary>
    /// 
    /// </summary>
    public struct MPos:IEquatable<MPos>
    {

        public readonly int U, V;

        public MPos(int u,int v) { U = u; V = v; }

        public static readonly MPos Zero = new MPos(0, 0);

        public static bool operator ==(MPos me,MPos other) { return me.U == other.U && me.V == other.V; }

        public static bool operator !=(MPos me,MPos other) { return !(me == other); }

        public bool Equals(MPos mPos)
        {
            return mPos.U == this.U && mPos.V == this.V;
        }

        public override bool Equals(object obj)
        {
            return obj is MPos && Equals((MPos)obj);
        }

        public override int GetHashCode()
        {
            return U.GetHashCode() ^ V.GetHashCode();
        }

        public CPos ToCPos(Map map)
        {
            return ToCPos(map.Grid.Type);
        }

        public CPos ToCPos(MapGridT gridT)
        {
            //TODO
            if (gridT == MapGridT.Rectangular)
                return new CPos(U, V);

            var offset = (V & 1) == 1 ? 1 : 0;
            var y = (V - offset) / 2 - U;
            var x = V - y;
            return new CPos(x, y);
            
        }

        public MPos Clamp(Rectangle r)
        {
            return new MPos(Math.Min(r.Right, Math.Max(U, r.Left)), 
                            Math.Min(r.Bottom,Math.Max(V,r.Top)));
        }

    }

    /// <summary>
    /// PPos ��ñ���Ϊ������Ļ�ռ���Ӧ�õĵ�Ԫ���
    /// ���в�ͬ���θ߶ȵĶ����Ԫ�����ͶӰ����ͬ��PPos�ϣ�
    /// ����ͶӰ�����PPos(������ǲ�����Ļ�������)
    /// PPos ������Ҫ���ڵ�ͼ��Ե�������ֿɼ��Բ�ѯ
    /// PPos is best thought of as a cell grid applied in screen space.
    /// Multiple cells with different terrain heights may be projected to the same PPOs,or to multiple PPos if they do not align with
    /// the screen grid.PPos coordinates are used primarily for map edge checks and shroud /visibility queries.
    /// </summary>
    public struct PPos:IEquatable<PPos>
    {
        public readonly int U, V;

        public static readonly PPos Zero = new PPos(0, 0);
        public PPos(int u,int v) { U = u; V = v; }

        public static bool operator ==(PPos a,PPos b)
        {
            return a.U == b.U && a.V == b.V;
        }

        public static bool operator !=(PPos a,PPos b)
        {
            return !(a == b);
        }

        public static explicit operator MPos(PPos puv) { return new MPos(puv.U, puv.V); }
        public static explicit operator PPos(MPos uv) { return new PPos(uv.U, uv.V); }

        public PPos Clamp(Rectangle r)
        {
            return new PPos(Math.Min(r.Right, Math.Max(U, r.Left)), Math.Min(r.Bottom, Math.Max(V, r.Top)));
        }


        public bool Equals(PPos other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return U.GetHashCode() ^ V.GetHashCode();
        }
    }
}