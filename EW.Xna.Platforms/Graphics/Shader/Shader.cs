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

        public int parameter;
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

        public int[] CBuffers { get; private set; }




        internal Shader(GraphicsDevice device,BinaryReader reader)
        {
            GraphicsDevice = device;

            var isVertexShader = reader.ReadBoolean();
            Stage = isVertexShader ? ShaderStage.Vertex : ShaderStage.Pixel;

            var shaderLength = reader.ReadInt32();
            var shaderByteCode = reader.ReadBytes(shaderLength);

            var samplerCount = (int)reader.ReadByte();
            Samplers = new SamplerInfo[samplerCount];

            for(var s = 0; s < samplerCount; s++)
            {
                var samplerInfo = Samplers[s];
                samplerInfo.type = (SamplerT)reader.ReadByte();
                samplerInfo.textureSlot = reader.ReadByte();
                samplerInfo.samplerSlot = reader.ReadByte();

                if (reader.ReadBoolean())
                {
                    samplerInfo.state = new SamplerState();
                    samplerInfo.state.AddressU = (TextureAddressMode)reader.ReadByte();
                    samplerInfo.state.AddressV = (TextureAddressMode)reader.ReadByte();
                    samplerInfo.state.AddressW = (TextureAddressMode)reader.ReadByte();
                    samplerInfo.state.BorderColor = new Color(reader.ReadByte(), 
                        reader.ReadByte(),
                        reader.ReadByte(),
                        reader.ReadByte());
                    samplerInfo.state.Filter = (TextureFilter)reader.ReadByte();
                    samplerInfo.state.MaxAnisotropy = reader.ReadInt32();
                    samplerInfo.state.MaxMipLevel = reader.ReadInt32();
                    samplerInfo.state.MipMapLevelOfDetailBias = reader.ReadSingle();
                }
                samplerInfo.name = reader.ReadString();
                samplerInfo.parameter = reader.ReadByte();
            }

            var cbufferCount = (int)reader.ReadByte();
            CBuffers = new int[cbufferCount];
            for(var c = 0; c < cbufferCount; c++)
            {
                CBuffers[c] = reader.ReadByte();
            }

            var attributeCount = (int)reader.ReadByte();
            Attributes = new VertexAttribute[attributeCount];
            for(var a = 0; a < attributeCount; a++)
            {
                Attributes[a].name = reader.ReadString();
                Attributes[a].usage = (VertexElementUsage)reader.ReadByte();
                Attributes[a].index = reader.ReadByte();
                Attributes[a].location = reader.ReadInt16();
            }
            PlatformConstruct(isVertexShader, shaderByteCode);
        }

        protected internal override void GraphicsDeviceResetting()
        {
            PlatformGraphicsDeviceResetting();
        }
    }
}