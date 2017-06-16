using System;
using System.Collections.Generic;
using System.IO;
using EW.Graphics;
using EW.FileSystem;
using EW.Primitives;
using EW.Xna.Platforms.Graphics;
using System.Linq;
namespace EW
{

    /// <summary>
    /// 
    /// </summary>
    public sealed class ModData:IDisposable
    {
        public IEnumerable<string> Languages { get; private set; }

        int initialThreadId;

        internal bool IsOnMainThread
        {
            get { return System.Threading.Thread.CurrentThread.ManagedThreadId == initialThreadId; }
        }
        public readonly Manifest Manifest;

        public readonly ObjectCreator ObjectCreator;

        public readonly MapCache MapCache;

        public readonly ISpriteLoader[] SpriteLoaders;
        public readonly ISpriteSequenceLoader SpriteSequenceLoader;
        public readonly GraphicsDevice Device;
        public ILoadScreen LoadScreen { get; private set; }
        /// <summary>
        /// Ĭ�Ϲ��� 
        /// </summary>
        readonly Lazy<Ruleset> defaultRules;
        
        public Ruleset DefaultRules { get { return defaultRules.Value; } }
        public EW.FileSystem.FileSystem ModFiles;
        public IReadOnlyFileSystem DefaultFileSystem { get { return ModFiles; } }

        readonly Lazy<IReadOnlyDictionary<string, TileSet>> defaultTileSets;

        public IReadOnlyDictionary<string,TileSet> DefaultTileSets
        {
            get
            {
                return defaultTileSets.Value;
            }
        }

        readonly Lazy<IReadOnlyDictionary<string, SequenceProvider>> defaultSequences;

        public IReadOnlyDictionary<string,SequenceProvider> DefaultSequences
        {
            get { return defaultSequences.Value; }
        }
        public ModData(GraphicsDevice device,Manifest mod,InstalledMods mods,bool useLoadScreen = false) : this(mod, mods, useLoadScreen)
        {
            this.Device = device;
        }

        public ModData(Manifest mod,InstalledMods mods,bool useLoadScreen = false)
        {
            Languages = new string[0];

            ModFiles = new FileSystem.FileSystem(mods);

            //local copy of the manifest
            Manifest = new Manifest(mod.Id, mod.Package);
            ModFiles.LoadFromManifest(Manifest);

            ObjectCreator = new ObjectCreator(Manifest, ModFiles);
            Manifest.LoadCustomData(ObjectCreator);

            if (useLoadScreen)
            {
                LoadScreen = ObjectCreator.CreateObject<ILoadScreen>(Manifest.LoadScreen.Value);
                LoadScreen.Init(this, Manifest.LoadScreen.ToDictionary(my => my.Value));
                LoadScreen.Display();
            }


            MapCache = new MapCache(this);

            SpriteLoaders = GetLoaders<ISpriteLoader>(Manifest.SpriteFormats, "sprite");

            var sequenceFormat = Manifest.Get<SpriteSequenceFormat>();
            var sequenceLoader = ObjectCreator.FindType(sequenceFormat.Type + "Loader");
            var ctor = sequenceLoader != null ? sequenceLoader.GetConstructor(new[] { typeof(ModData) }) : null;
            if (sequenceLoader == null || !sequenceLoader.GetInterfaces().Contains(typeof(ISpriteSequenceLoader)) || ctor == null)
                throw new InvalidOperationException("Unable to find a sequence loader for type '{0}'.".F(sequenceFormat.Type));
            SpriteSequenceLoader = (ISpriteSequenceLoader)ctor.Invoke(new[] { this });
            SpriteSequenceLoader.OnMissingSpriteError =s=> { };
            
            defaultRules = Exts.Lazy(() => Ruleset.LoadDefaults(this));

            //������Ƭ��
            defaultTileSets = Exts.Lazy(() =>
            {
                var items = new Dictionary<string, TileSet>();
                foreach(var file in Manifest.TileSets)
                {
                    var t = new TileSet(DefaultFileSystem, file);
                    items.Add(t.Id, t);
                }
                return (IReadOnlyDictionary<string,TileSet>)(new ReadOnlyDictionary<string, TileSet>(items));
            });

            //���м�
            defaultSequences = Exts.Lazy(() => {

                var items = DefaultTileSets.ToDictionary(t => t.Key, t => new SequenceProvider(DefaultFileSystem, this, t.Value, null));
                return (IReadOnlyDictionary<string,SequenceProvider>)(new ReadOnlyDictionary<string, SequenceProvider>(items));

            });

            initialThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        public void InitializeLoaders(IReadOnlyFileSystem fileSystem)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TLoader"></typeparam>
        /// <param name="formats"></param>
        /// <param name="name"></param>
        /// <returns></returns>

        TLoader[] GetLoaders<TLoader>(IEnumerable<string> formats,string name)
        {
            var loaders = new List<TLoader>();
            foreach(var format in formats)
            {
                var loader = ObjectCreator.FindType(format + "Loader");
                if (loader == null || !loader.GetInterfaces().Contains(typeof(TLoader)))
                    throw new InvalidOperationException("Unable to find a {0} loader for type '{1}'.".F(name, format));

                loaders.Add((TLoader)ObjectCreator.CreateBasic(loader));
            }
            return loaders.ToArray();
        }

        /// <summary>
        /// ��ͼ׼��
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public Map PrepareMap(string uid)
        {
            if (MapCache[uid].Status != MapStatus.Available)
                throw new InvalidDataException("Invalid map uid:{0}".F(uid));

            Map map;
            using (new Support.PerfTimer("Map"))
                map = new Map(this, MapCache[uid].Package);

            using (new Support.PerfTimer("Map.Music"))
                foreach (var entry in map.Rules.Music)
                    entry.Value.Load(map);


            return map;
        }


        internal void HandleLoadingProgress()
        {
            if(LoadScreen != null && IsOnMainThread)
            {
                LoadScreen.Display();
            }
        }





        public void Dispose()
        {
            MapCache.Dispose();

        }


    }



    public interface ILoadScreen : IDisposable
    {
        void Init(ModData m, Dictionary<string, string> info);

        void Display();

        bool RequiredContentIsInstalled();

        void StartGame(Arguments args);
    }
}