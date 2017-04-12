using System;
using OpenTK.Graphics.ES20;

namespace EW.Xna.Platforms.Graphics
{

    public enum RenderTargetUsage
    {
        /// <summary>
        /// ��ȾĿ�����ݽ����ᱣ��
        /// </summary>
        DiscardContents,

        /// <summary>
        /// 
        /// </summary>
        PreserveContents,

        /// <summary>
        /// 
        /// </summary>
        PlatformContents,

    }


    /// <summary>
    /// ��ȾĿ��
    /// </summary>
    internal interface IRenderTarget
    {

        /// <summary>
        /// 
        /// </summary>
        int Width { get; }

        int Height { get; }

        RenderTargetUsage RenderTargetUsage { get; }


        int GLTexture { get; }

        TextureTarget GLTarget { get; }

        int GLColorBuffer { get; set; }

        int GLDepthBuffer { get; set; }

        int GLStencilBuffer { get; set; }

        int MultiSampleCount { get; }

        int LevelCount { get; }

        TextureTarget GetFramebufferTarget(RenderTargetBinding renderTargetBinding);

    }
}