using System;

namespace EW.Xna.Platforms.Graphics
{
    /// <summary>
    /// Defines usage for vertexx elements;
    /// </summary>
    public enum VertexElementUsage
    {
        Position,           //位置
        Color,              //颜色
        TextureCoordinate,//纹理坐标
        Normal,             //法线
        Binormal,           //次法线
        Tangent,            //切线
        BlendIndices,
        BlendWeight,
        Depth,              //深度
        Fog,
        PointSize,
        Sample,
        TessellateFactor    //

    }

    /// <summary>
    /// 
    /// 
    /// </summary>
    public enum VertexElementFormat
    {
        Single,
        Vector2,
        Vector3,
        Vector4,
        Color,
        Byte4,
        Short2,//tow signed 16-bit integer
        Short4,
        NormalizedShort2,
        NormalizedShort4,

    }

    /// <summary>
    /// 顶点元素
    /// </summary>
    public struct VertexElement:IEquatable<VertexElement>
    {

        private int _offset;
        public int Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        private VertexElementFormat _format;
        public VertexElementFormat VertexElementFormat
        {
            get { return _format; }
            set { _format = value; }
        }

        private VertexElementUsage _usage;

        public VertexElementUsage VertexElementUsage
        {
            get { return _usage; }
            set { _usage = value; }
        }

        private int _usageIndex;

        public int UsageIndex
        {
            get { return _usageIndex; }
            set { _usageIndex = value; }
        }

        public VertexElement(int offset,VertexElementFormat elementFormat,VertexElementUsage elementUsage,int usageIndex)
        {
            _offset = offset;
            _format = elementFormat;
            _usageIndex = usageIndex;
            _usage = elementUsage;
        }

        public bool Equals(VertexElement element)
        {
            return _offset == element._offset &&
                    _format == element._format &&
                    _usage == element._usage &&
                    _usageIndex == element._usageIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is VertexElement && Equals((VertexElement)obj);
        }

        public override int GetHashCode()
        {
            int hashCode = _offset;
            hashCode ^= (int)_format << 9;
            hashCode ^= (int)_usage << (9 + 4);
            hashCode ^= _usageIndex << (9 + 4 + 4);
            return hashCode;
        }
    }
}