using System;
using System.Collections.Generic;
using System.IO;

namespace EW.Xna.Platforms.Graphics
{

    internal enum ShaderStage
    {
        Vertex,//����
        Pixel,  //����
    }

    /// <summary>
    /// ����������
    /// </summary>
    internal enum SamplerT
    {
        Sampler2D = 0,
        SamplerCube = 1,
        SamplerVolume = 2,
        Sampler1D = 3,
    }

    /// <summary>
    /// ��������Ϣ
    /// </summary>
    internal struct SamplerInfo
    {
        public SamplerT type;
        public int textureSlot;
        public int samplerSlot;
        public string name;
        public SamplerState state;

    }

    /// <summary>
    /// ��������
    /// </summary>
    internal struct VertexAttribute
    {
        public VertexElementUsage usage;
        public int index;
        public string name;
        public int location;
    }

    /// <summary>
    /// ��ɫ��
    /// </summary>
    internal partial class Shader:GraphicsResource
    {

        internal int HashKey { get; private set; }

        public SamplerInfo[] Samplers { get; private set; }
        public ShaderStage Stage { get; private set; }

        public VertexAttribute[] Attributes { get; private set; }



        internal Shader(GraphicsDevice device,BinaryReader reader)
        {
            GraphicsDevice = device;

            var isVertexShader = reader.ReadBoolean();
            Stage = isVertexShader ? ShaderStage.Vertex : ShaderStage.Pixel;

            var shaderLength = reader.ReadInt32();
            var shaderByteCode = reader.ReadBytes(shaderLength);

            PlatformConstruct(isVertexShader, shaderByteCode);
        }

        protected internal override void GraphicsDeviceResetting()
        {
            PlatformGraphicsDeviceResetting();
        }
    }
}