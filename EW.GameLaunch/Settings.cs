using System;
using System.Collections.Generic;
using System.IO;
using Java.IO;
using EW.Traits;
using System.Linq;
using EW.Graphics;
namespace EW
{
    public enum StatusBarsType { Standard,DamageShow,AlwaysShow}

    public class PlayerSettings
    {
        public string Name = "Newbie";
        public HSLColor Color = new HSLColor(75, 255, 180);
        public string LastServer = "localhost:1234";
    }

    public class ServerSettings
    {
        [Desc("Sets the server name.")]
        public string Name = "";

        [Desc("Sets the internal port")]
        public int ListenPort = 1234;

        [Desc("Sets the port advertised to the master server.")]
        public int ExternalPort = 1234;

        [Desc("Report the game to the master server list.")]
        public bool AdvertiseOnline = true;

        [Desc("Locks the game with a password.")]
        public string Password = "";

        public bool DiscoverNatDevices = false;

        public int NatDiscoveryTimeout = 1000;

        public string Map = null;

        public string[] Ban = { };

        public bool EnableSingleplayer = false;

        public bool QueryMapRepository = true;

        public string TimestampFormat = "s";


        public ServerSettings Clone()
        {
            return (ServerSettings)MemberwiseClone();
        }
    }


    public class DebugSettings
    {
        public bool LuaDebug = false;
        public bool PerfText = false;
        public bool PerfGraph = false;

        public bool SanityCheckUnsyncedCode = false;

        public string UUID = System.Guid.NewGuid().ToString();


        public bool StrictActivityChecking = false;
        public int Samples = 25;
    }


    public class GraphicsSettings
    {
        public int SheetSize = 2048;
        public int BatchSize = 4096;

        public string Language = "english";
        public string DefaultLanguage = "china";

        public bool PixelDouble;

        /// <summary>
        /// Add a frame rate limiter.
        /// </summary>
        public bool CapFramerate = true;

        /// <summary>
        /// At whick frames per second to cap the framerate.
        /// </summary>
        public int MaxFramerate = 60;
    }
    /// <summary>
    /// 
    /// </summary>
    public class GameSettings
    {
        public string Mod = "modchooser";
        public string PreviousMod = "ra";

        public bool AllowZoom = true;

        public bool ShowShellmap = true;

        public bool DrawTargetLine = true;

        public int SelectionDeadzone = 24;

        public bool UsePlayerStanceColor = false;
        public bool UseClassicMouseStyle = false;

        public StatusBarsType StatusBars = StatusBarsType.Standard;

        public bool AllowDownloading = true;

        public float UIScrollSpeed = 50f;
    }

    public class SoundSettings
    {
        public float SoundVolume = 0.5f;
        public float MusicVolume = 0.5f;
        public float VideoVolume = 0.5f;

        public bool Mute = false;
        public bool CashTicks = true;

        public bool Repeat = false;

        public bool Shuffle = false;
    }
    
    public class Settings
    {
        string settingFile;
        public readonly PlayerSettings Player = new PlayerSettings();
        public readonly GraphicsSettings Graphics = new GraphicsSettings();
        public readonly GameSettings Game = new GameSettings();
        public readonly SoundSettings Sound = new SoundSettings();
        public readonly DebugSettings Debug = new DebugSettings();
        public readonly ServerSettings Server = new ServerSettings();
        public Dictionary<string, object> Sections;

        public Settings(string file,Arguments args)
        {
            settingFile = file;
            Sections = new Dictionary<string, object>()
            {
                {"Player",Player },
                {"Game",Game },
                {"Sound",Sound },
                {"Debug",Debug },
                {"Graphics",Graphics},
                {"Server",Server}
            };

            var err1 = FieldLoader.UnknownFieldAction;
            var err2 = FieldLoader.InvalidValueAction;

            try
            {
                
                var stream = Android.App.Application.Context.Assets.Open("Content/settings.yaml");
                 //if (File.Exists(settingFile))
                {
                    var yaml = MiniYaml.DictFromStream(stream);
                    foreach(var kv in Sections)
                    {
                        if (yaml.ContainsKey(kv.Key))
                            LoadSectionYaml(yaml[kv.Key], kv.Value);
                    }
                }
            }
            finally
            {

            }
        }

        static void LoadSectionYaml(MiniYaml yaml,object section)
        {
            FieldLoader.Load(section, yaml);
        }


        public static string SanitizedPlayerName(string dirty)
        {
            var forbiddenNames = new string[] { "Open", "Closed" };
            var botNames = WarGame.ModData.DefaultRules.Actors["player"].TraitInfos<IBotInfo>().Select(t => t.Name);

            var clean = SanitizedName(dirty);

            if (string.IsNullOrWhiteSpace(clean) || forbiddenNames.Contains(clean) || botNames.Contains(clean))
                clean = new PlayerSettings().Name;

            if (clean.Length > 16)
                clean = clean.Substring(0, 16);

            return clean;

        }

        static string SanitizedName(string dirty)
        {
            if (string.IsNullOrEmpty(dirty))
                return null;

            var clean = dirty;


            // reserved characters for MiniYAML and JSON
            var disallowedChars = new char[] { '#', '@', ':', '\n', '\t', '[', ']', '{', '}', '"', '`' };
            foreach (var disallowedChar in disallowedChars)
                clean = clean.Replace(disallowedChar.ToString(), string.Empty);

            return clean;
        }
    }
}