using System;

namespace EW.Xna.Platforms.Graphics
{
    /// <summary>
    /// 
    /// </summary>
    internal partial class GraphicsCapabilities
    {

        public GraphicsCapabilities(GraphicsDevice graphicsDevice)
        {
            
        }
        internal bool SupportsNonPowerOfTwo { get; private set; }

        /// <summary>
        /// Gets the support for S3TC (DXT1, DXT3, DXT5)
        /// </summary>
        internal bool SupportsS3tc { get; private set; }

        internal bool SupportsDxt1 { get; private set; }

        internal bool SupportsPvrtc { get; private set; }

        internal bool SupportsEtc1 { get; private set; }

        internal bool SupportsTextrueArrays { get; private set; }

        internal bool SupportsVertexTextures { get; private set; }

        internal bool SupportsDepthClamp { get; private set; }

        internal bool SupportsTextureMaxLevel { get; private set; }

        /// <summary>
        /// Whether the device supports anisotropic texture filtering
        /// </summary>
		internal bool SupportsTextureFilterAnisotropic { get; private set; }


        internal bool SupportsDepth24 { get; private set; }

        internal bool SupportsDepthNonLinear { get; private set; }

        internal bool SupportsPackedDepthStencil { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        internal void Initialize(GraphicsDevice device)
        {
            PlatformInitialize(device);
        }
    }
}