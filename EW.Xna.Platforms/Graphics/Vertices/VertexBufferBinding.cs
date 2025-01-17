using System;

namespace EW.Xna.Platforms.Graphics
{
    /// <summary>
    /// Defines how a vertex buffer is bound to the graphics device for rendering.
    /// </summary>
    public struct VertexBufferBinding
    {


        private readonly VertexBuffer _vertexBuffer;

        /// <summary>
        /// Gets the vertex buffer
        /// </summary>
        public VertexBuffer VertexBuffer { get { return _vertexBuffer; } }

        private readonly int _vertexOffset;


        /// <summary>
        /// Gets the index of the first vertex in the vertex buffer to use
        /// </summary>
        public int VertexOffset { get { return _vertexOffset; } }

        private readonly int _instanceFrequency;


        /// <summary>
        /// The Number of instances to draw using the same per-instance data before advancing in the buffer by one element.
        /// 
        /// </summary>
        public int InstanceFrequency
        {
            get { return _instanceFrequency; }
        }


        public VertexBufferBinding(VertexBuffer vertexBuffer) : this(vertexBuffer, 0, 0)
        {

        }

        public VertexBufferBinding(VertexBuffer vertexBuffer,int vertexOffset) : this(vertexBuffer, vertexOffset, 0) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="vertexBuffer"></param>
        /// <param name="vertexOffset"></param>
        /// <param name="instanceFrequency"></param>
        public VertexBufferBinding(VertexBuffer vertexBuffer,int vertexOffset,int instanceFrequency)
        {
            if (vertexBuffer == null)
                throw new ArgumentNullException("vertexBuffer");
            if (vertexOffset < 0 || vertexOffset >= vertexBuffer.VertexCount)
                throw new ArgumentOutOfRangeException("vertexOffset");

            if (instanceFrequency < 0)
                throw new ArgumentOutOfRangeException("instanceFrequency");
            _vertexBuffer = vertexBuffer;
            _vertexOffset = vertexOffset;
            _instanceFrequency = instanceFrequency;
        }
    }
}