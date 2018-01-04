using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using EW.Framework;
using EW.Framework.Graphics;
using EW.Support;
using EW.Graphics;
using EW.NetWork;
using EW.Primitives;
namespace EW
{
    /// <summary>
    /// Our game is derived from the class EW.Xna.Framework.Game 
    /// and is the heart of our application. The Game class is responsible for initializing the graphics device,
    /// loading content and most importantly, running the application game loop. 
    /// The majority of our code is implemented by overriding several of Game��s protected methods.
    /// </summary>
    public class WarGame:Game
    {
        /// <summary>
        /// 120 ms net tick for 40ms local tick
        /// </summary>
        public const int NetTickScale = 3;
        public const int Timestep = 40;
        public const int TimestepJankThreshold = 250;

        public static MersenneTwister CosmeticRandom = new MersenneTwister();
        public static ModData ModData;
        public static Settings Settings;
        public static InstalledMods Mods { get; private set; }
   
        public GraphicsDeviceManager DeviceManager;


        static Stopwatch stopwatch = Stopwatch.StartNew();

        public static long RunTime
        {
            get
            {
                return stopwatch.ElapsedMilliseconds;
            }
        }

        public static Sound Sound;
        public static Renderer Renderer;
        static WorldRenderer worldRenderer;
        internal static OrderManager orderManager;
        static volatile ActionQueue delayedActions = new ActionQueue();

        public static void RunAfterTick(Action a)
        {
            delayedActions.Add(a, RunTime);
        }

        public static void RunAfterDelay(int delayMilliseconds,Action a)
        {
            delayedActions.Add(a, RunTime + delayMilliseconds);
                
        }


        public static int LocalTick { get { return orderManager.LocalFrameNumber; } }

        public int NetFrameNumber { get { return orderManager.NetFrameNumber; } }


        public static int RenderFrame = 0;
        public WarGame() {

            IsFixedTimeStep = true;
            DeviceManager = new GraphicsDeviceManager(this);
            DeviceManager.DeviceCreated += (object sender, EventArgs args) => {

                Initialize(new Arguments());
            };
            DeviceManager.IsFullScreen = true;
            DeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
        }

        protected override void Initialize()
        {
            base.Initialize();
            //Initialize(new Arguments());
        }
        

        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="args"></param>
        internal void Initialize(Arguments args)
        {
            string customModPath = null;
            orderManager = new OrderManager();
            InitializeSettings(args);

            customModPath = Android.App.Application.Context.FilesDir.Path;
            Mods = new InstalledMods(customModPath);

            InitializeMod(Settings.Game.Mod, args);
        }

        /// <summary>
        /// ��ʼ�������ļ�
        /// </summary>
        /// <param name="args"></param>
        public static void InitializeSettings(Arguments args)
        {
            Settings = new Settings(Platform.ResolvePath(Path.Combine("^","settings.yaml")), args);
        }
        /// <summary>
        /// ��ʼ��Mod
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="args"></param>
        public void InitializeMod(string mod,Arguments args)
        {
            if (ModData != null)
            {

                ModData = null;
            }

            if (worldRenderer != null)
                worldRenderer.Dispose();
            worldRenderer = null;

            if (orderManager != null)
                orderManager.Dispose();

            if(ModData != null)
            {
                ModData.ModFiles.UnmountAll();
                ModData.Dispose();
            }

            if (mod == null)
                throw new InvalidOperationException("Game.Mod argument missing.");

            if (!Mods.ContainsKey(mod))
                throw new InvalidOperationException("Unknown or invalid mod '{0}'.".F(mod));

            Renderer = new Renderer(Settings.Graphics,GraphicsDevice);
            Sound = new Sound(Settings.Sound);
            Sound.StopVideo();
            ModData = new ModData(Mods[mod], Mods, true);

            using (new Support.PerfTimer("LoadMaps"))
                ModData.MapCache.LoadMaps();

            ModData.InitializeLoaders(ModData.DefaultFileSystem);

            var grid = ModData.Manifest.Contains<MapGrid>() ? ModData.Manifest.Get<MapGrid>() : null;
            Renderer.InitializeDepthBuffer(grid);
            ModData.LoadScreen.StartGame(args);
            //LoadShellMap();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void LoadShellMap()
        {
            var shellmap = ChooseShellMap();
            using (new PerfTimer("StartGame"))
                StartGame(shellmap, WorldT.Shellmap);
        }

        static string ChooseShellMap()
        {
            var shellMaps = ModData.MapCache.Where(m => m.Status == MapStatus.Available && m.Visibility.HasFlag(MapVisibility.Shellmap)).Select(m => m.Uid);

            if (!shellMaps.Any())
                throw new InvalidDataException("No valid shellmaps available");

            return shellMaps.Random(CosmeticRandom);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapUID"></param>
        /// <param name="type"></param>
        internal static void StartGame(string mapUID,WorldT type)
        {

            if (worldRenderer != null)
                worldRenderer.Dispose();
            Map map;
            using (new PerfTimer("PrepareMap"))
                map = ModData.PrepareMap(mapUID);
            using (new PerfTimer("NewWorld"))
                orderManager.World = new World(ModData,map, orderManager, type);

            worldRenderer = new WorldRenderer(ModData, orderManager.World);

            using (new PerfTimer("LoadComplete"))
                orderManager.World.LoadComplete(worldRenderer);

            if (orderManager.GameStarted)
                return;

            orderManager.LocalFrameNumber = 0;
            orderManager.LastTickTime = RunTime;
            orderManager.StartGame();
            worldRenderer.RefreshPalette();
            GC.Collect();
            CustomRun();
        }
        

        protected override void Update(GameTime gameTime)
        {
            //LogicTick(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            //Console.WriteLine("gametime:" + gameTime.ElapsedGameTime.TotalMilliseconds);
            //RenderTick();
            //GraphicsDevice.Clear(Color.Yellow);
        }

        /// <summary>
        /// 
        /// </summary>
        static void RenderTick()
        {
            using (new PerfSample("render"))
            {
                ++RenderFrame;
                if (worldRenderer != null)
                {
                    Renderer.BeginFrame(worldRenderer.ViewPort.TopLeft, worldRenderer.ViewPort.Zoom);
                    Sound.SetListenerPosition(worldRenderer.ViewPort.CenterPosition.ToVector3());
                    worldRenderer.Draw();

                }
                else
                    Renderer.BeginFrame(Int2.Zero, 1f);

                using(new PerfSample("render_widgets"))
                {
                    //Renderer.WorldModelRenderer.BeginFrame();

                    //Renderer.WorldModelRenderer.EndFrame();
                }

                using (new PerfSample("render_flip"))
                    Renderer.EndFrame();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static void LogicTick(GameTime time=null)
        {
            delayedActions.PerformActions(RunTime);


            InnerLogicTick(orderManager,time);
        }

        /// <summary>
        /// �ڲ��߼�
        /// </summary>
        /// <param name="orderManager"></param>
        static void InnerLogicTick(OrderManager orderManager,GameTime time)
        {
            //var tick = (long)time.TotalGameTime.TotalMilliseconds;
            var tick = RunTime;

            var world = orderManager.World;

            var worldTimestep = world == null ? Timestep : world.Timestep;

            var worldTickDelta = tick - orderManager.LastTickTime;

            if(worldTimestep != 0 && worldTickDelta >= worldTimestep)
            {
                using(new PerfSample("tick_time"))
                {
                    //Tick the world to advance the world time to match real time:

                    var integralTickTimestep = (worldTickDelta / worldTimestep) * worldTimestep;
                    orderManager.LastTickTime += integralTickTimestep >= TimestepJankThreshold ? integralTickTimestep : worldTimestep;
                    Sound.Tick();
                    if (world == null) return;
                    //Don't tick when the shellmap is disabled
                    if (world.ShouldTick)
                    {
                        var isNetTick = LocalTick % NetTickScale == 0;
                        if (!isNetTick || orderManager.IsReadyForNextFrame)
                        {
                            ++orderManager.LocalFrameNumber;

                            if (isNetTick)
                                orderManager.Tick();

                            world.Tick();
                            PerfHistory.Tick();
                        }
                        else if (orderManager.NetFrameNumber == 0)
                            orderManager.LastTickTime = RunTime;

                        //Wait until we have done our first world Tick before TickRendering

                        if (orderManager.LocalFrameNumber > 0)
                            Sync.CheckSyncUnchanged(world, () => world.TickRender(worldRenderer));
                    }
                }
            }
        }

        public static T CreateObject<T>(string name)
        {
            return ModData.ObjectCreator.CreateObject<T>(name);
        }


        internal static RunStatus CustomRun()
        {
            try
            {
                Loop();
            }
            catch(Exception exp)
            {
                throw exp;
            }
            finally
            {
                if (orderManager != null)
                    orderManager.Dispose();

                if (worldRenderer != null)
                    worldRenderer.Dispose();

                ModData.Dispose();
                Renderer.Dispose();

            }
            return state;
        }


        static RunStatus state = RunStatus.Running;
        static void Loop()
        {
            const int MaxLogicTicksBehind = 250;

            const int MinReplayFps = 10;

            //Timestamps for when the next logic and rendering should run
            var nextLogic = RunTime;
            var nextRender = RunTime;
            var forcedNextRender = RunTime;

            while(state == RunStatus.Running)
            {
                //Ideal time between logic updates.Timestep = 0 means the game is paused
                //but we still call LogicTick() because it handles pausing internally.
                var logicInterval = worldRenderer != null && worldRenderer.World.Timestep != 0 ? worldRenderer.World.Timestep : Timestep;

                //Ideal time between screen updates.
                var maxFramerate = Settings.Graphics.CapFramerate ? Settings.Graphics.MaxFramerate.Clamp(1, 1000) : 1000;
                var renderInterval = 1000 / maxFramerate;

                var now = RunTime;

                //If the logic has fallen behind to much,skip it and catch up
                if (now - nextLogic > MaxLogicTicksBehind)
                    nextLogic = now;

                var nextUpdate = Math.Min(nextLogic, nextRender);
                if (now >= nextUpdate)
                {
                    var forceRender = now >= forcedNextRender;

                    if(now >= nextLogic)
                    {
                        nextLogic += logicInterval;

                        LogicTick();

                        //Force at least one render per tick during regular gameplay
                        if (orderManager.World != null && !orderManager.World.IsReplay)
                            forceRender = true;
                    }

                    var haveSomeTimeUntilNextLogic = now < nextLogic;
                    var isTimeToRender = now >= nextRender;

                    if ((isTimeToRender && haveSomeTimeUntilNextLogic) || forceRender)
                    {
                        nextRender = now + renderInterval;

                        var maxRenderInterval = Math.Max(1000 / MinReplayFps, renderInterval);
                        forcedNextRender = now + maxRenderInterval;
                        RenderTick();
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep((int)(nextUpdate - now));
                }

            }



        }
    }
}