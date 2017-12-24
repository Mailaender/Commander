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

        public bool Equals(MPos mPos)
        {
            return mPos.U == this.U && mPos.V == this.V;
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
    /// ͶӰ��ͼλ��
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