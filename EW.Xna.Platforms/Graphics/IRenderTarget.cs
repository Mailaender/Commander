using System;
using OpenTK.Graphics.ES20;

namespace EW.Xna.Platforms.Graphics
{

    public enum RenderTargetUsage
    {
        /// <summary>
        /// The render traget content will not be preserved.
        /// ��ȾĿ�����ݽ����ᱣ��
        /// </summary>
        DiscardContents,

        /// <summary>
        /// The render target content will be preserved even if it is slow or requires extra memory.
        /// ��ʹ��������Ҫ���������ڴ棬��ȾĿ�����ݽ�������
        /// </summary>
        PreserveContents,

        /// <summary>
        /// The render target content might be preserved if the platform can do so without a penalty in performance or memory usage.
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