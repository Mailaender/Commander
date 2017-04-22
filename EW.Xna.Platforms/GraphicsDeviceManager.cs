using System;
using EW.Xna.Platforms.Graphics;
using EW.Xna.Platforms.Input.Touch;
#if ANDROID
using Android.Views;
#endif
namespace EW.Xna.Platforms
{
    public interface IGraphicsDeviceService
    {
        GraphicsDevice GraphicsDevice { get; }

    }

    public class PreparingDeviceSettingsEventArgs : EventArgs
    {
        public GraphicsDeviceInformation GraphicsDeviceInformation;

        public PreparingDeviceSettingsEventArgs(GraphicsDeviceInformation information)
        {
            GraphicsDeviceInformation = information;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class GraphicsDeviceManager:IGraphicsDeviceManager,IGraphicsDeviceService,IDisposable
    {
        readonly Game _game;
        private GraphicsDevice _graphicsDevice;         //ͼ�λ����豸
        private DisplayOrientation _supportedOrientation;

        private bool _wantFullScreen = false;

        public bool IsFullScreen
        {
            get
            {
                if (_graphicsDevice != null)
                    return _graphicsDevice.PresentationParameters.IsFullScreen;
                return _wantFullScreen;
            }
            set
            {
                _wantFullScreen = value;
                if(_graphicsDevice!=null)
                {
                    _graphicsDevice.PresentationParameters.IsFullScreen = value;
                }
#if ANDROID
                ForceSetFullScreen();
#endif
            }
        }


        private int _preferredBackBufferWidth;

        public int PreferredBackBufferWidth
        {
            get
            {
                return _preferredBackBufferWidth;
            }
            set
            {
                _preferredBackBufferWidth = value;
            }
        }

        public int PreferredBackBufferHeight
        {
            get
            {
                return _preferredBackBufferHeight;
            }
            set
            {
                _preferredBackBufferHeight = value;
            }
        }
        private int _preferredBackBufferHeight;

        private DepthFormat _preferredDepthStencilFormat;

        public DepthFormat PreferredDepthStencilFormat
        {
            get { return _preferredDepthStencilFormat; }
            set { _preferredDepthStencilFormat = value; }
        }


        public static readonly int DefaultBackBufferWidth = 800;
        public static readonly int DefaultBackBufferHeight = 480;

        private bool _drawBegun;
        bool disposed;

        public GraphicsProfile GraphicsProfile { get; set; }

        public GraphicsDevice GraphicsDevice
        {
            get
            {
                return _graphicsDevice;
            }
        }
        public GraphicsDeviceManager(Game game)
        {
            if (game == null)
                throw new ArgumentNullException("The game cannot be null");
            _game = game;
            _supportedOrientation = DisplayOrientation.Default;

            if (_game.Services.GetService(typeof(IGraphicsDeviceManager)) != null)
                throw new ArgumentException("Graphics Device Manager Already Present");

            _game.Services.AddService(typeof(IGraphicsDeviceManager),this);
            _game.Services.AddService(typeof(IGraphicsDeviceService), this);
        }

        ~GraphicsDeviceManager()
        {
            Dispose(false);
        }

#if ANDROID

        internal void ForceSetFullScreen()
        {
            if (IsFullScreen)
            {
                Game.Activity.Window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
                Game.Activity.Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            }
            else
            {
                Game.Activity.Window.SetFlags(WindowManagerFlags.ForceNotFullscreen, WindowManagerFlags.ForceNotFullscreen);
                    
            }
        }
#endif


        public DisplayOrientation SupportedOrientations
        {
            get { return _supportedOrientation; }
            set
            {
                _supportedOrientation = value;
                if (_game.Window != null)
                    _game.Window.SetSupportedOrientations(_supportedOrientation);
            }
        }

        /// <summary>
        /// �����豸
        /// </summary>
        public void CreateDevice()
        {
            Initialize();
            OnDeviceCreated(EventArgs.Empty);
        }


        /// <summary>
        /// ��ʼ������ͼ���豸
        /// </summary>
        private void Initialize()
        {
            var presentationParameters = new PresentationParameters();
            presentationParameters.DepthStencilFormat = DepthFormat.Depth24;

            if(PreparingDeviceSettings != null)
            {
                GraphicsDeviceInformation deviceInfo = new GraphicsDeviceInformation();
                deviceInfo.GraphicsProfile = GraphicsProfile;
                deviceInfo.Adapter = GraphicsAdapter.DefaultAdapter;
                deviceInfo.PresentationParameters = presentationParameters;

                PreparingDeviceSettingsEventArgs evtArgs = new PreparingDeviceSettingsEventArgs(deviceInfo);
                PreparingDeviceSettings(this, evtArgs);
                //presentationParameters = evtArgs.GraphicsDeviceInformation.PresentationParameters;
                //GraphicsProfile = evtArgs.GraphicsDeviceInformation.GraphicsProfile;
            }
            _graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile, presentationParameters);

            ApplyChanges();

            TouchPanel.DisplayWidth = _graphicsDevice.PresentationParameters.BackBufferWidth;
            TouchPanel.DisplayHeight = _graphicsDevice.PresentationParameters.BackBufferHeight;
            TouchPanel.DisplayOrientation = _graphicsDevice.PresentationParameters.DisplayOrientation;
        }



        public bool BeginDraw()
        {
            if (_graphicsDevice == null)
                return false;
            _drawBegun = true;
            return true;
        }

        public void EndDraw()
        {
            if(_graphicsDevice !=null && _drawBegun)
            {
                _drawBegun = false;
                _graphicsDevice.Present();
            }
        }

        /// <summary>
        /// ���ÿͻ��˱߽�
        /// </summary>
        internal void ResetClientBounds()
        {
#if ANDROID







#endif
        }


        /// <summary>
        /// Ӧ�øı�
        /// </summary>
        public void ApplyChanges()
        {
            if (_graphicsDevice == null)
                return;
#if ANDROID
            ((AndroidGameWindow)_game.Window).SetOrientation(_game.Window.CurrentOrientation,false);
#endif

            _graphicsDevice.PresentationParameters.DisplayOrientation = _game.Window.CurrentOrientation;

            bool isLandScape = (0 != (_game.Window.CurrentOrientation & (DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight)));

            int w = PreferredBackBufferWidth;
            int h = PreferredBackBufferHeight;

            _graphicsDevice.PresentationParameters.BackBufferWidth = isLandScape ? Math.Max(w, h) : Math.Min(w,h);
            _graphicsDevice.PresentationParameters.BackBufferHeight = isLandScape ? Math.Max(w, h) : Math.Min(w, h);

            ResetClientBounds();

            TouchPanel.DisplayWidth = _graphicsDevice.PresentationParameters.BackBufferWidth;
            TouchPanel.DisplayHeight = _graphicsDevice.PresentationParameters.BackBufferHeight;

        }

        #region IGraphicsDeviceService Members

        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceReseting;

        public event EventHandler<PreparingDeviceSettingsEventArgs> PreparingDeviceSettings;
        internal void OnDeviceDisposing(EventArgs ea)
        {
            Raise(DeviceDisposing, ea);
        }

        internal void OnDeviceReseting(EventArgs ea)
        {
            Raise(DeviceReseting, ea);
        }

        internal void OnDeviceReset(EventArgs ea)
        {
            Raise(DeviceReset, ea);
        }
        internal void OnDeviceCreated(EventArgs ea)
        {
            Raise(DeviceCreated, ea);
        }



        private void Raise<TEventArgs>(EventHandler<TEventArgs> handler,TEventArgs evt) where TEventArgs : EventArgs
        {
            if (handler != null)
                handler(this, evt);
        }
        #endregion


        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if(_graphicsDevice != null)
                    {
                        _graphicsDevice.Dispose();
                        _graphicsDevice = null;
                    }
                }
                disposed = true;
            }
        }

        #endregion


    }
}