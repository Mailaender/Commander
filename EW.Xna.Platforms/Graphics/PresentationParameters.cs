using System;
using System.Collections.Generic;

namespace EW.Xna.Platforms.Graphics
{
    /// <summary>
    /// ͼ����������
    /// </summary>
    public class PresentationParameters:IDisposable
    {
        public const int DefaultPresentRate = 60;//Ĭ��֡��

        public PresentationParameters()
        {
            Clear();
        }

        public void Clear()
        {
            backBufferFormat = SurfaceFormat.Color;
            _backBufferWidth = GraphicsDeviceManager.DefaultBackBufferWidth;
            _backBufferHeight = GraphicsDeviceManager.DefaultBackBufferHeight;

            deviceWindowHandle = IntPtr.Zero;

            _depthStencilFormat = DepthFormat.None;
            DisplayOrientation = DisplayOrientation.Default;
        }

        private SurfaceFormat backBufferFormat;
        public SurfaceFormat BackBufferFormat
        {
            get { return backBufferFormat; }
            set
            {
                backBufferFormat = value;
            }
        }

        private DepthFormat _depthStencilFormat;

        public DepthFormat DepthStencilFormat
        {
            get { return _depthStencilFormat; }
            set { _depthStencilFormat = value; }
        }

        private bool _isFullScreen;
        public bool IsFullScreen
        {
            get
            {
                return _isFullScreen;
            }
            set
            {
                _isFullScreen = value;
            }
        }
        private int _backBufferWidth = GraphicsDeviceManager.DefaultBackBufferWidth;
        private int _backBufferHeight = GraphicsDeviceManager.DefaultBackBufferHeight;

        public int BackBufferWidth
        {
            get { return _backBufferWidth; }
            set { _backBufferWidth = value; }
        }

        public int BackBufferHeight
        {
            get { return _backBufferHeight; }
            set { _backBufferHeight = value; }
        }
        private IntPtr deviceWindowHandle;

        public DisplayOrientation DisplayOrientation { get; set; }

        public RenderTargetUsage RenderTargetUsage { get; set; }
        public void Dispose() { }

    }
}