using System;
using OpenTK.Graphics.ES20;
namespace EW.Xna.Platforms.Graphics
{
    public enum CullMode
    {
        None,
        /// <summary>
        /// ˳ʱ��
        /// </summary>
        CullClockwiseFace,

        /// <summary>
        /// ��ʱ��
        /// </summary>
        CullCounterClockwiseFace,
    }
    public partial class RasterizerState
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        /// <param name="force"></param>
        internal void PlatformApplyState(GraphicsDevice device,bool force = false)
        {
            var offscreen = device.IsRenderTargetBound;

            if (force)
            {
                GL.Disable(EnableCap.Dither);
            }

            if(CullMode == CullMode.None)
            {
                GL.Disable(EnableCap.CullFace);
                GraphicsExtensions.CheckGLError();
            }
            else
            {
                GL.Enable(EnableCap.CullFace);
                GraphicsExtensions.CheckGLError();
                GL.CullFace(CullFaceMode.Back);//   ֻ�޳�����
                GraphicsExtensions.CheckGLError();

                if(CullMode == CullMode.CullClockwiseFace)
                {
                    if (offscreen)
                        GL.FrontFace(FrontFaceDirection.Cw);    //˳ʱ�뷽��Ϊ����
                    else
                        GL.FrontFace(FrontFaceDirection.Ccw);   //��ʱ�뷽��Ϊ����

                    GraphicsExtensions.CheckGLError();
                
                }
                else
                {
                    if (offscreen)
                        GL.FrontFace(FrontFaceDirection.Ccw);
                    else
                        GL.FrontFace(FrontFaceDirection.Cw);
                    GraphicsExtensions.CheckGLError();
                }


            }

            if(force || this.ScissorTestEnable != device._lastRasterizerState.ScissorTestEnable)
            {
                if (ScissorTestEnable)
                    GL.Enable(EnableCap.ScissorTest);
                else
                    GL.Disable(EnableCap.ScissorTest);

                GraphicsExtensions.CheckGLError();
                device._lastRasterizerState.ScissorTestEnable = this.ScissorTestEnable;
            }

           
        }


    }
}