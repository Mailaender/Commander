using System;
using System.IO;
using System.Linq;
using EW.Xna.Platforms;
using EW.Xna.Platforms.Graphics;
using EW.Support;
using EW.Graphics;
using EW.NetWork;
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

        public static Renderer Renderer;
        WorldRenderer worldRenderer;
        OrderManager orderManager;

        public int LocalTick { get { return orderManager.LocalFrameNumber; } }

        public int NetFrameNumber { get { return orderManager.NetFrameNumber; } }


        public static int RenderFrame = 0;
        public WarGame() {
            DeviceManager = new GraphicsDeviceManager(this);
            DeviceManager.IsFullScreen = true;
            DeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
        }

        protected override void Initialize()
        {
            base.Initialize();
            Initialize(new Arguments());
        }

        protected override void BeginRun()
        {
            base.BeginRun();
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

            Renderer = new Renderer(this,Settings.Graphics);
            ModData = new ModData(this,Mods[mod], Mods, true);

            using (new Support.PerfTimer("LoadMaps"))
                ModData.MapCache.LoadMaps();

            ModData.InitializeLoaders(ModData.DefaultFileSystem);

            var grid = ModData.Manifest.Contains<MapGrid>() ? ModData.Manifest.Get<MapGrid>() : null;
            Renderer.InitializeDepthBuffer(grid);
            //ModData.LoadScreen.StartGame(args);
            LoadShellMap();
        }

        /// <summary>
        /// 
        /// </summary>
        public void LoadShellMap()
        {
            var shellmap = ChooseShellMap();
            using (new PerfTimer("StartGame"))
                StartGame(shellmap, WorldT.Shellmap);
        }

        string ChooseShellMap()
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
        internal void StartGame(string mapUID,WorldT type)
        {

            if (worldRenderer != null)
                worldRenderer.Dispose();
            Map map;
            using (new PerfTimer("PrepareMap"))
                map = ModData.PrepareMap(mapUID);
            using (new PerfTimer("NewWorld"))
                orderManager.World = new World(map, orderManager, type);

            worldRenderer = new WorldRenderer(this,ModData, orderManager.World);

            using (new PerfTimer("LoadComplete"))
                orderManager.World.LoadComplete(worldRenderer);

            if (orderManager.GameStarted)
                return;

            orderManager.LocalFrameNumber = 0;
            orderManager.LastTickTime = RunTime;
            orderManager.StartGame();
            worldRenderer.RefreshPalette();
            GC.Collect();
        }


        protected override void LoadContent()
        {
            this.Components.Add(Renderer);

            //spriteBatch = new SpriteBatch(this.GraphicsDevice);

        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }




        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            LogicTick(gameTime);
        }

        private BasicEffect _effect;
        private VertexBuffer _vb;
        private SpriteBatch spriteBatch;
        private VertexBufferBinding[] bindings = new VertexBufferBinding[1];
        protected override void Draw(GameTime gameTime)
        {
            

            base.Draw(gameTime);
            RenderTick();

            //GraphicsDevice.Clear(Color.CornflowerBlue);
            //spriteBatch.Begin();
            //Vector2 topLeftOfSprite = new Vector2(150, 150);
            //Color tintColor = Color.White;
            //spriteBatch.Draw(Renderer.currentPaletteTexture, topLeftOfSprite, tintColor);
            //spriteBatch.End();



            //GraphicsDevice.Clear(Color.CornflowerBlue);

            //if (_effect == null)
            //{
            //    _effect = new BasicEffect(GraphicsDevice);

            //    var vp = GraphicsDevice.Viewport;
            //    Matrix projection;
            //    Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1, out projection);
            //    _effect.Projection = projection;
            //}

            //_effect.World = Matrix.Identity;
            //_effect.DiffuseColor = Color.Red.ToVector3();
            //_effect.CurrentTechnique.Passes[0].Apply();
            //if (_vb == null)
            //{
            //    _vb = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration, 6, BufferUsage.WriteOnly);
            //    _vb.SetData(new[]
            //    {
            //        new VertexPositionColor(new Vector3(100,100,0),Color.White),
            //        new VertexPositionColor(new Vector3(200,100,0),Color.White),
            //        new VertexPositionColor(new Vector3(200,200,0),Color.White),
            //        new VertexPositionColor(new Vector3(100,200,0),Color.White),
            //        new VertexPositionColor(new Vector3(100,100,0),Color.White),
            //        new VertexPositionColor(new Vector3(200,200,0),Color.White)

            //    });
            //    //GraphicsDevice.SetVertexBuffer(_vb);

            //    bindings[0] = new VertexBufferBinding(_vb);
            //    GraphicsDevice.SetVertexBuffers(bindings);
            //}

            //GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
        }

        /// <summary>
        /// 
        /// </summary>
        void RenderTick()
        {
            using (new PerfSample("render"))
            {
                ++RenderFrame;
                if (worldRenderer != null)
                {
                    Renderer.BeginFrame(worldRenderer.ViewPort.TopLeft, worldRenderer.ViewPort.Zoom);

                    worldRenderer.Draw();

                }
                else
                    Renderer.BeginFrame(Int2.Zero, 1f);

                using (new PerfSample("render_flip"))
                    Renderer.EndFrame();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void LogicTick(GameTime time)
        {
            InnerLogicTick(orderManager,time);
        }

        /// <summary>
        /// �ڲ��߼�
        /// </summary>
        /// <param name="orderManager"></param>
        void InnerLogicTick(OrderManager orderManager,GameTime time)
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

                    var integralTickTimestep = (worldTickDelta / worldTimestep) * worldTimestep;
                    orderManager.LastTickTime += integralTickTimestep >= TimestepJankThreshold ? integralTickTimestep : worldTimestep;

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
                    
                    }
                }
            }
        }

        public static T CreateObject<T>(string name)
        {
            return ModData.ObjectCreator.CreateObject<T>(name);
        }
    }
}