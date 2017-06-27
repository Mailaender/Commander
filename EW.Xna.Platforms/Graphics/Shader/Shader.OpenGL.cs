using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.ES20;
using Bool = OpenTK.Graphics.ES20.All;
namespace EW.Xna.Platforms.Graphics
{

    
    internal partial class Shader
    {
        private string _glslCode;//��ɫ��Դ��

        //
        private int _shaderHandler = -1;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="isVertexShader"></param>
        /// <param name="shaderBytecode"></param>
        private void PlatformConstruct(bool isVertexShader,byte[] shaderBytecode)
        {
            _glslCode = System.Text.Encoding.ASCII.GetString(shaderBytecode);

//            if (isVertexShader)
//            {
//                _glslCode = @"
//                                #ifdef GL_ES
//                                precision highp float;
//                                precision mediump int;
//                                #endif

//                                uniform vec3 Scroll;
//                                uniform vec3 r1, r2;

//                                attribute vec4 aVertexPosition;
//                                attribute vec4 aVertexTexCoord;
//                                attribute vec2 aVertexTexMetadata;
//                                varying vec4 vTexCoord;
//                                varying vec2 vTexMetadata;
//                                varying vec4 vChannelMask;
//                                varying vec4 vDepthMask;

//                                vec4 DecodeChannelMask(float x)
//                                {
//	                                if (x > 0.7)
//		                                return vec4(0,0,0,1);
//	                                if (x > 0.5)
//		                                return vec4(0,0,1,0);
//	                                if (x > 0.3)
//		                                return vec4(0,1,0,0);
//	                                else
//		                                return vec4(1,0,0,0);
//                                }

//                                void main()
//                                {
//	                                gl_Position = vec4((aVertexPosition.xyz - Scroll.xyz) * r1 + r2, 1);
//	                                vTexCoord = aVertexTexCoord;
//	                                vTexMetadata = aVertexTexMetadata;
//	                                vChannelMask = DecodeChannelMask(abs(aVertexTexMetadata.t));
//	                                if (aVertexTexMetadata.t < 0.0)
//	                                {
//		                                float x = -aVertexTexMetadata.t * 10.0;
//		                                vDepthMask = DecodeChannelMask(x - floor(x));
//	                                }
//	                                else
//		                                vDepthMask = vec4(0,0,0,0);
//                                } 
//";

//            }
//            else
//            {
//                _glslCode = @"

//                    #extension GL_EXT_frag_depth : enable
//                    #ifdef GL_ES
//                    precision mediump float;
//                    precision mediump int;
//                    #endif
//                    uniform sampler2D DiffuseTexture, Palette;
                    
//                    uniform bool EnableDepthPreview;
//                    uniform float DepthTextureScale;

//                    varying vec4 vTexCoord;
//                    varying vec2 vTexMetadata;
//                    varying vec4 vChannelMask;
//                    varying vec4 vDepthMask;

//                    float jet_r(float x)
//                    {
//	                    return x < 0.7 ? 4.0 * x - 1.5 : -4.0 * x + 4.5;
//                    }

//                    float jet_g(float x)
//                    {
//	                    return x < 0.5 ? 4.0 * x - 0.5 : -4.0 * x + 3.5;
//                    }

//                    float jet_b(float x)
//                    {
//	                    return x < 0.3 ? 4.0 * x + 0.5 : -4.0 * x + 2.5;
//                    }

//                    void main()
//                    {
//	                    vec4 x = texture2D(DiffuseTexture, vTexCoord.st);
//	                    vec2 p = vec2(dot(x, vChannelMask), vTexMetadata.s);
//	                    vec4 c = texture2D(Palette, p);
//	                    if (c.a == 0.0)
//		                    discard;

//	                    float depth = gl_FragCoord.z;
//	                    if (length(vDepthMask) > 0.0)
//	                    {
//		                    vec4 y = texture2D(DiffuseTexture, vTexCoord.pq);
//		                    depth = depth + DepthTextureScale * dot(y, vDepthMask);
//	                    }
//	                    gl_FragDepthEXT = 0.5 * depth + 0.5;

//	                    if (EnableDepthPreview)
//	                    {
//		                    float x = 1.0 - gl_FragDepthEXT;
//		                    float r = clamp(jet_r(x), 0.0, 1.0);
//		                    float g = clamp(jet_g(x), 0.0, 1.0);
//		                    float b = clamp(jet_b(x), 0.0, 1.0);
//		                    gl_FragColor = vec4(r, g, b, 1.0);
//	                    }
//	                    else
//		                    gl_FragColor = c;
//                    }
//                ";
//            }
            HashKey = EW.Xna.Platforms.Utilities.Hash.ComputeHash(shaderBytecode);
        }
        /// <summary>
        /// ��ȡ��ɫ��
        /// </summary>
        /// <returns></returns>
        internal int GetShaderHandle()
        {
            if (_shaderHandler != -1)
                return _shaderHandler;
            //������ɫ��(����&Ƭ��)
            _shaderHandler = GL.CreateShader(Stage == ShaderStage.Vertex ? ShaderType.VertexShader : ShaderType.FragmentShader);
            GraphicsExtensions.CheckGLError();
            //��ɫ��Դ�븽�ӵ���ɫ����
            GL.ShaderSource(_shaderHandler, _glslCode);
            GraphicsExtensions.CheckGLError();
            //������ɫ��
            GL.CompileShader(_shaderHandler);
            GraphicsExtensions.CheckGLError();
            //����һ�����α��α�ʶ�����Ƿ�ɹ�
            int compiled = 0;
            GL.GetShader(_shaderHandler, ShaderParameter.CompileStatus, out compiled);
            GraphicsExtensions.CheckGLError();
            if(compiled != (int)Bool.True)
            {
                var log = GL.GetShaderInfoLog(_shaderHandler);
                Console.WriteLine(log);
                if (GL.IsShader(_shaderHandler))
                {
                    GL.DeleteShader(_shaderHandler);
                    GraphicsExtensions.CheckGLError();
                }
                _shaderHandler = -1;

                throw new InvalidOperationException("Shader Compilation Failed");
            }
            return _shaderHandler;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="usage"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal int GetAttribLocation(VertexElementUsage usage,int index)
        {
            for (int i =0; i < Attributes.Length; i++)
            {
                if((Attributes[i].usage == usage) && (Attributes[i].index == index))
                {
                    return Attributes[i].location;
                }
            }
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="program"></param>
        internal void GetVertexAttributeLocations(int program)
        {
            for(int i = 0; i < Attributes.Length; i++)
            {
                Attributes[i].location = GL.GetAttribLocation(program, Attributes[i].name);
                GraphicsExtensions.CheckGLError();
            }
        }

        internal void ApplySamplerTextureUnits(int program)
        {
            foreach(var sampler in Samplers)
            {
                var loc = GL.GetUniformLocation(program, sampler.name);
                GraphicsExtensions.CheckGLError();
                if (loc != -1)
                {
                    GL.Uniform1(loc, sampler.textureSlot);
                    GraphicsExtensions.CheckGLError();
                }
            }
        }


        /// <summary>
        /// ����ƽ̨�ϵ�ͼ���豸
        /// </summary>
        private void PlatformGraphicsDeviceResetting()
        {
            if(_shaderHandler != -1)
            {
                if (GL.IsShader(_shaderHandler))
                {
                    GL.DeleteShader(_shaderHandler);
                    GraphicsExtensions.CheckGLError();
                }
                _shaderHandler = -1;
            }
        }

        protected override void Dispose(bool disposing)
        {

            if(!IsDisposed && _shaderHandler != -1)
            {
                Threading.BlockOnUIThread(() =>
                {

                    GL.DeleteShader(_shaderHandler);
                    GraphicsExtensions.CheckGLError();
                    _shaderHandler = -1;
                });
            }

            base.Dispose(disposing);
        }



    }
}