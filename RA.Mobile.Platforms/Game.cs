using System;
using System.Diagnostics;
using RA.Mobile.Platforms.Graphics;
using RA.Mobile.Platforms.Input.Touch;
namespace RA.Mobile.Platforms
{
    public class Game:IDisposable
    {

#if ANDROID
        [CLSCompliant(false)]
        public static AndroidGameActivity Activity { get; internal set; }
#endif
        private bool _isDisposed;
        private static Game _instance = null;

        private TimeSpan _accumulatedElapsedTime;
        private readonly GameTime _gameTime = new GameTime();
        private Stopwatch _gameTimer;
        private long _previousTicks = 0;
        private int _updateFrameLag;

        private bool _isFixedTimeStep;
        public bool IsFixedTimeStep
        {
            get { return _isFixedTimeStep; }
            set { _isFixedTimeStep = value; }
        }

        private TimeSpan _targetElapsedTime = TimeSpan.FromTicks(166667);//60 fps

        public TimeSpan TargetElapsedTime
        {
            get { return _targetElapsedTime; }
            set
            {
                value = Platform.TargetElapsedTimeChanging(value);
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("The time must be positive and non-zero,", default(Exception));
                }
                if(value != _targetElapsedTime)
                {
                    _targetElapsedTime = value;
                    Platform.TargetElapsedTimeChanged();
                }
            }
        }

        private TimeSpan _maxElapsedTime = TimeSpan.FromMilliseconds(500);




        private TimeSpan _inactiveSleepTime = TimeSpan.FromSeconds(0.02f);
        internal static Game Instance
        {
            get
            {
                return Game._instance;
            }
        }

        public Game()
        {
            _instance = this;
            _services = new GameServiceContainer();

            Platform = GamePlatform.PlatformCreate(this);
            Platform.Activated += OnActivated;
            Platform.Deactivated += OnDeactivated;
            _services.AddService(typeof(GamePlatform), Platform);
        }

        ~Game()
        {
            Dispose(false);
        }

        
        internal GamePlatform Platform;
        private GameServiceContainer _services;
        public GameServiceContainer Services
        {
            get
            {
                return _services;
            }
        }
        private IGraphicsDeviceService _graphicsDeviceService;

        public GraphicsDevice GraphicsDevice
        {
            get
            {
                if(_graphicsDeviceService == null)
                {
                    _graphicsDeviceService = (IGraphicsDeviceService)Services.GetService(typeof(IGraphicsDeviceService));
                    if (_graphicsDeviceService == null)
                        throw new InvalidOperationException("No Graphics Device Service");

                }
                return _graphicsDeviceService.GraphicsDevice;
            }
        }


        private IGraphicsDeviceManager _graphicsDeviceManager;

        internal GraphicsDeviceManager graphicsDeviceManager
        {
            get
            {
                if(_graphicsDeviceManager == null)
                {
                    _graphicsDeviceManager = (IGraphicsDeviceManager)Services.GetService(typeof(IGraphicsDeviceManager));
                    if (_graphicsDeviceManager == null)
                        throw new InvalidOperationException("No Graphics Device Manager");
                }
                return (GraphicsDeviceManager)_graphicsDeviceManager;
            }
        }
        public GameWindow Window
        {
            get
            {
                return Platform.Window;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sneder"></param>
        /// <param name="args"></param>
        protected virtual void OnActivated(object sneder,EventArgs args)
        {

        }

        protected virtual void OnDeactivated(object sender,EventArgs args)
        {

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="manager"></param>
        internal void ApplyChanges(GraphicsDeviceManager manager)
        {

        }


        protected virtual void Initialize()
        {
            ApplyChanges(graphicsDeviceManager);

            _graphicsDeviceService = (IGraphicsDeviceService)Services.GetService(typeof(IGraphicsDeviceService));

        }



        /// <summary>
        /// 
        /// </summary>
        internal void DoInitialize()
        {
            AssertNotDisposed();
            Platform.BeforeInitialize();

        }


        private void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                string name = GetType().Name;
                throw new ObjectDisposedException(name, string.Format("The {0} object was used after being Disposed.", name));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Tick()
        {
            RetryTick:
            var currentTicks = _gameTimer.Elapsed.Ticks;
            _accumulatedElapsedTime += TimeSpan.FromTicks(currentTicks-_previousTicks);
            _previousTicks = currentTicks;
            if(IsFixedTimeStep && _accumulatedElapsedTime < TargetElapsedTime)
            {
                var sleepTime = (int)(TargetElapsedTime - _accumulatedElapsedTime).TotalMilliseconds;
                System.Threading.Thread.Sleep(sleepTime);
                goto RetryTick;
            }

            if(_accumulatedElapsedTime > _maxElapsedTime)
            {
                _accumulatedElapsedTime = _maxElapsedTime;
            }

            if (IsFixedTimeStep)
            {
                _gameTime.ElapsedGameTime = TargetElapsedTime;
                var stepCount = 0;
                while(_accumulatedElapsedTime >= TargetElapsedTime)
                {
                    _gameTime.TotalGameTime += TargetElapsedTime;
                    _accumulatedElapsedTime -= TargetElapsedTime;
                    ++stepCount;
                    DoUpdate(_gameTime);
                }

                _updateFrameLag += Math.Max(0, stepCount - 1);
                if (_gameTime.IsRunningSlowly)
                {
                    if (_updateFrameLag == 0)
                        _gameTime.IsRunningSlowly = false;
                }
                else if(_updateFrameLag >= 5)
                {
                    _gameTime.IsRunningSlowly = true;
                }

                if (stepCount == 1 && _updateFrameLag > 0)
                    _updateFrameLag--;

                _gameTime.ElapsedGameTime = TimeSpan.FromTicks(TargetElapsedTime.Ticks * stepCount);
            }
            else
            {

            }


        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameTime"></param>
        internal void  DoUpdate(GameTime gameTime)
        {
            AssertNotDisposed();
            if (Platform.BeforeUpdate(gameTime))
            {
                Update(gameTime);

                TouchPanelState.CurrentTimestamp = gameTime.TotalGameTime;

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameTime"></param>
        protected virtual void Update(GameTime gameTime)
        {

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if(_graphicsDeviceManager != null)
                    {
                        (_graphicsDeviceManager as GraphicsDeviceManager).Dispose();
                        _graphicsDeviceManager = null;
                    }

                    if(Platform != null)
                    {
                        Platform.Activated -= OnActivated;
                        Platform.Deactivated -= OnDeactivated;
                        _services.RemoveService(typeof(GamePlatform));

                        Platform.Dispose();
                        Platform = null;
                    }
                }

#if ANDROID
                Activity = null;
#endif
                _isDisposed = true;
                _instance = null;
            }
        }
    }
}