using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Drawing;
using EW.FileSystem;
using EW.Traits;
namespace EW
{
    /// <summary>
    /// ͷ�ļ���ʽ
    /// </summary>
    struct BinaryDataHeader
    {
        public readonly byte Format;
        public readonly uint TilesOffset;
        public readonly uint HeightsOffset;
        public readonly uint ResourcesOffset;


        public BinaryDataHeader(Stream s,EW.OpenGLES.Point expectedSize)
        {
            Format = s.ReadUInt8();
            var width = s.ReadUInt16();
            var height = s.ReadUInt16();
            if(width!= expectedSize.X || height != expectedSize.Y)
            {
                throw new InvalidDataException("Invalid tile data");
            }

            if (Format == 1)
            {
                TilesOffset = 5;
                HeightsOffset = 0;
                ResourcesOffset = (uint)(3 * width * height + 5);
            }
            else if (Format == 2)
            {
                TilesOffset = s.ReadUInt32();
                HeightsOffset = s.ReadUInt32();
                ResourcesOffset = s.ReadUInt32();

            }
            else
                throw new InvalidDataException("Unknown binary map format '{0}'".F(Format));
        }
    }


    public enum MapVisibility
    {
        Lobby = 1,
        Shellmap = 2,
        MissionSelector = 4,
    }
    class MapField
    {
        enum Type
        {
            Noraml,
            NodeList,
            MiniYaml,
        }

        readonly FieldInfo field;
        readonly PropertyInfo property;
        readonly Type type;

        readonly string key;
        readonly string fieldName;
        readonly bool required;
        readonly string ignoreIfValue;
            
        public MapField(string key,string fieldName = null,bool required = true,string ignoreIfValue = null)
        {
            this.key = key;
            this.fieldName = fieldName ?? key;
            this.required = required;
            this.ignoreIfValue = ignoreIfValue;

            field = typeof(Map).GetField(this.fieldName);
            property = typeof(Map).GetProperty(this.fieldName);

            if (field == null && property == null)
                throw new InvalidOperationException("Map does not have a field/property {0}".F(fieldName));

            var t = field != null ? field.FieldType : property.PropertyType;

            type = t == typeof(List<MiniYamlNode>) ? Type.NodeList : t == typeof(MiniYaml) ? Type.MiniYaml : Type.Noraml;

                

        }

        /// <summary>
        /// �����л�
        /// </summary>
        /// <param name="map"></param>
        /// <param name="nodes"></param>
        public void Deserialize(Map map,List<MiniYamlNode> nodes)
        {
            var node = nodes.FirstOrDefault(n => n.Key == key);
            if(node == null)
            {
                if (required)
                    throw new YamlException("Required field '{0}' not found in map.yaml".F(key));
                return;
            }

            if (field != null)
            {
                if (type == Type.NodeList)
                    field.SetValue(map, node.Value.Nodes);
                else if (type == Type.MiniYaml)
                    field.SetValue(map, node.Value);
                else
                    FieldLoader.LoadField(map, fieldName, node.Value.Value);
            }

            if(property != null)
            {
                if (type == Type.NodeList)
                    property.SetValue(map, node.Value.Nodes, null);
                else if (type == Type.MiniYaml)
                    property.SetValue(map, node.Value, null);
                else
                    FieldLoader.LoadField(map, fieldName, node.Value.Value);
            }

                
        }


    }
    /// <summary>
    /// 
    /// </summary>
    public class Map:IReadOnlyFileSystem
    {

        //Format versions
        public int MapFormat { get; private set; }
        public readonly byte TileFormat = 2;

        public const int SupportedMapFormat = 11;

        static readonly MapField[] YamlFields =
        {
            new MapField("MapFormat"),
            new MapField("RequiresMod"),
            new MapField("Title"),
            new MapField("Author"),
            new MapField("Tileset"),
            new MapField("MapSize"),
            new MapField("Bounds"),
            new MapField("Visibility"),
            new MapField("Categories"),
            new MapField("LockPreview",required:false,ignoreIfValue:"False"),
            new MapField("Players","PlayerDefinitions"),
            new MapField("Actors","ActorDefinitions"),
            new MapField("Rules","RuleDefinitions",required:false),
            new MapField("Sequences","SequenceDefinitions",required:false),
            new MapField("ModelSequences","ModelSequenceDefinitions",required:false),
            new MapField("Weapons","WeaponDefinitions",required:false),
        };


        //Standard yaml metadata
        public string RequiresMod;
        public string Title;
        public string Author;
        public string Tileset;
        public bool LockPreview;
        public EW.OpenGLES.Rectangle Bounds;
        public MapVisibility Visibility = MapVisibility.Lobby;
        public string[] Categories = { "Conquest" };

        //
        public List<MiniYamlNode> PlayerDefinitions = new List<MiniYamlNode>();
        public List<MiniYamlNode> ActorDefinitions = new List<MiniYamlNode>();

        public readonly MiniYaml RuleDefinitions;
        public readonly MiniYaml SequenceDefinitions;
        public readonly MiniYaml ModelSequenceDefinitions;
        public readonly MiniYaml WeaponDefinitions;
        public readonly MiniYaml VoicDefinitions;
        public readonly MiniYaml NotificationDefinitions;
        public readonly MiniYaml MusicDefinitions;

        //Internal data
        readonly ModData modData;
        bool initializedCellProjection;
        CellLayer<PPos[]> cellProjection;
        CellLayer<List<MPos>> inverseCellProjection;
        CellLayer<short> cachedTerrainIndexes;



        public string Uid { get; private set; }
        /// <summary>
        /// ��ͼ�������� 
        /// </summary>
        public readonly MapGrid Grid;

        public IReadOnlyPackage Package { get; private set; }

        public EW.OpenGLES.Point MapSize { get; private set; }

        public Ruleset Rules { get; private set; }
        
        public ProjectedCellRegion ProjectedCellBounds { get; private set;}
        
        public WPos ProjectedBottomRight { get; private set; }

        public WPos ProjectedTopLeft { get; private set; }

        public CellLayer<TerrainTile> Tiles { get; private set; }

        public CellLayer<ResourceTile> Resources { get; private set; }

        public CellLayer<byte> Height { get; private set; }

        public CellLayer<byte> CustomTerrain { get; private set; }

        public CellRegion AllCells { get; private set; }

        public List<CPos> AllEdgeCells { get; private set; }
        public Map(ModData modData,IReadOnlyPackage package)
        {
            this.modData = modData;
            Package = package;

            if (!Package.Contains("map.yaml") || !Package.Contains("map.bin"))
                throw new InvalidDataException("Not a valid map\n File:{0}".F(package.Name));

            var yaml = new MiniYaml(null, MiniYaml.FromStream(Package.GetStream("map.yaml"), package.Name));
            foreach(var field in YamlFields)
            {
                field.Deserialize(this, yaml.Nodes);
            }

            if(MapFormat != SupportedMapFormat)
            {
                throw new InvalidDataException("Map format {0} is not supported. \n File:{1}".F(MapFormat, package.Name));
            }
            PlayerDefinitions = MiniYaml.NodesOrEmpty(yaml, "Players");
            ActorDefinitions = MiniYaml.NodesOrEmpty(yaml, "Actors");

            Grid = modData.Manifest.Get<MapGrid>();

            var size = new Size((int)MapSize.X, (int)MapSize.Y);

            //Layer
            Tiles = new CellLayer<TerrainTile>(Grid.Type, size);
            Resources = new CellLayer<ResourceTile>(Grid.Type, size);
            Height = new CellLayer<byte>(Grid.Type, size);
            

            using(var s = Package.GetStream("map.bin"))
            {
                var header = new BinaryDataHeader(s, MapSize);

                if (header.TilesOffset > 0)
                {
                    s.Position = header.TilesOffset;
                    for(var i = 0; i < MapSize.X; i++)
                    {
                        for(var j = 0; j < MapSize.Y; j++)
                        {
                            var tile = s.ReadUInt16();
                            var index = s.ReadUInt8();
                            if (index == byte.MaxValue)
                                index = (byte)(i % 4 + (j % 4) * 4);

                            Tiles[new MPos(i, j)] = new TerrainTile(tile, index);
                        }
                    }
                }

                if(header.ResourcesOffset > 0)
                {
                    s.Position = header.ResourcesOffset;
                    for(var i = 0; i < MapSize.X; i++)
                    {
                        for(var j = 0; j < MapSize.Y; j++)
                        {
                            var type = s.ReadUInt8();
                            var density = s.ReadUInt8();
                            Resources[new MPos(i, j)] = new ResourceTile(type, density);
                        }
                    }
                }

                if (header.HeightsOffset > 0)
                {
                    s.Position = header.HeightsOffset;
                    for(var i = 0; i < MapSize.X; i++)
                    {
                        for(var j = 0; j < MapSize.Y; j++)
                        {
                            Height[new MPos(i, j)] = s.ReadUInt8().Clamp((byte)0, Grid.MaximumTerrainHeight);
                        }
                    }
                }
            }

            if (Grid.MaximumTerrainHeight > 0)
            {
                Tiles.CellEntryChanged += UpdateProjection;
                Height.CellEntryChanged += UpdateProjection;
            }

            PostInit();

            Uid = ComputeUID(Package);
            
            
        }

        public bool Contains(CPos cell)
        {
            if (Grid.Type == MapGridT.RectangularIsometric && cell.X < cell.Y)
                return false;
            return Contains(cell.ToMPos(this));
        }

        public bool Contains(MPos uv)
        {
            return CustomTerrain.Contains(uv) && ContainsAllProjectedCellsCovering(uv);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="uv"></param>
        /// <returns></returns>
        bool ContainsAllProjectedCellsCovering(MPos uv)
        {
            if (Grid.MaximumTerrainHeight == 0)
                return Contains((PPos)uv);

            var projectedCells = ProjectedCellsCovering(uv);
            if (projectedCells.Length == 0)
                return false;

            foreach (var puv in projectedCells)
                if (!Contains(puv))
                    return false;
            return true;
        }

        public bool Contains(PPos puv)
        {
            return Bounds.Contains(puv.U, puv.V);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cell"></param>
        void UpdateProjection(CPos cell)
        {
            MPos uv;
            if(Grid.MaximumTerrainHeight == 0)
            {
                uv = cell.ToMPos(Grid.Type);
                cellProjection[cell] = new[] { (PPos)uv };
                var inverse = inverseCellProjection[uv];
                inverse.Clear();
                inverse.Add(uv);
                return;
                   
            }

            if (!initializedCellProjection)
                InitializeCellPojection();

            uv = cell.ToMPos(Grid.Type);

            foreach (var puv in cellProjection[uv])
                inverseCellProjection[(MPos)puv].Remove(uv);

            var projected = ProjectCellInner(uv);
            cellProjection[uv] = projected;

            foreach (var puv in projected)
                inverseCellProjection[(MPos)puv].Add(uv);

        }

        static readonly PPos[] NoProjectedCells = { };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uv"></param>
        /// <returns></returns>
        PPos[] ProjectCellInner(MPos uv)
        {
            var mapHeight = Height;
            if (!mapHeight.Contains(uv))
                return NoProjectedCells;

            var height = mapHeight[uv];
            if (height == 0)
                return new[] { (PPos)uv };

            if((height & 1) == 1)
            {
                var ti = Rules.TileSet.GetTileInfo(Tiles[uv]);
                if (ti != null && ti.RampT != 0)
                    height += 1;
            }

            var candidates = new List<PPos>();

            if ((height & 1) == 1)
            {
                if ((uv.V & 1) == 1)
                    candidates.Add(new PPos(uv.U + 1, uv.V - height));
                else
                    candidates.Add(new PPos(uv.U - 1, uv.V - height));

                candidates.Add(new PPos(uv.U, uv.V - height));
                candidates.Add(new PPos(uv.U, uv.V - height + 1));
                candidates.Add(new PPos(uv.U, uv.V - height - 1));
            }
            else
                candidates.Add(new PPos(uv.U, uv.V - height));

            return candidates.Where(c => mapHeight.Contains((MPos)c)).ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uv"></param>
        /// <returns></returns>
        public PPos[] ProjectedCellsCovering(MPos uv)
        {
            if (!initializedCellProjection)
                InitializeCellPojection();

            if (!cellProjection.Contains(uv))
                return NoProjectedCells;

            return cellProjection[uv];
        }

        public PPos ProjectedCellCovering(WPos pos)
        {
            var projectedPos = pos - new WVec(0, pos.Z, pos.Z);

            return (PPos)CellContaining(projectedPos).ToMPos(Grid.Type);
        }

        /// <summary>
        /// 
        /// </summary>
        void InitializeCellPojection()
        {
            if (initializedCellProjection)
                return;

            initializedCellProjection = true;

            cellProjection = new CellLayer<PPos[]>(this);
            inverseCellProjection = new CellLayer<List<MPos>>(this);

            foreach(var cell in AllCells)
            {
                var uv = cell.ToMPos(Grid.Type);
                cellProjection[uv] = new PPos[0];
                inverseCellProjection[uv] = new List<MPos>();
                    
            }

            foreach (var cell in AllCells)
                UpdateProjection(cell);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public CPos CellContaining(WPos pos)
        {
            if (Grid.Type == MapGridT.Rectangular)
                return new CPos(pos.X / 1024, pos.Y / 1024);

            //Convert from world position to isometric cell postion;
            var u = (pos.Y + pos.X - 724) / 1448;
            var v = (pos.Y - pos.X + (pos.Y > pos.X ? 724 : -724)) / 1448;
            return new CPos(u, v);
        }

        /// <summary>
        /// Convert from isometric cell position to world position;
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public WPos CenterOfCell(CPos cell)
        {
            if (Grid.Type == MapGridT.Rectangular)
                return new WPos(1024 * cell.X + 512, 1024 * cell.Y + 512, 0);

            //
            var z = Height.Contains(cell) ? 724 * Height[cell] : 0;
            return new WPos(724 * (cell.X - cell.Y + 1), 724 * (cell.X + cell.Y + 1), z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="subCell"></param>
        /// <returns></returns>
        public WPos CenterOfSubCell(CPos cell,SubCell subCell)
        {
            var index = (int)subCell;
            if (index >= 0 && index <= Grid.SubCellOffsets.Length)
                return CenterOfCell(cell) + Grid.SubCellOffsets[index];
            return CenterOfCell(cell);
        }

        /// <summary>
        /// 
        /// </summary>
        void PostInit()
        {
            try
            {
                Rules = Ruleset.Load(modData, this, Tileset, RuleDefinitions, WeaponDefinitions, 
                    VoicDefinitions, NotificationDefinitions, MusicDefinitions, SequenceDefinitions,
                    ModelSequenceDefinitions);

            }
            catch (Exception e)
            {

                Rules = Ruleset.LoadDefaultsForTileSet(modData, Tileset);
            }

            Rules.Sequences.PreLoad();

            var tl = new MPos(0, 0).ToCPos(this);
            var br = new MPos(MapSize.X - 1, MapSize.Y - 1).ToCPos(this);

            AllCells = new CellRegion(Grid.Type, tl, br);

            var btl = new PPos(Bounds.Left, Bounds.Top);
            var bbr = new PPos(Bounds.Right - 1, Bounds.Bottom - 1);

            SetBounds(btl, bbr);

            CustomTerrain = new CellLayer<byte>(this);
            foreach(var uv in AllCells.MapCoords)
            {
                CustomTerrain[uv] = byte.MaxValue;
            }

            

        }

        

        public PPos Clamp(PPos puv)
        {
            var bounds = new EW.OpenGLES.Rectangle(Bounds.X, Bounds.Y, Bounds.Width - 1, Bounds.Height - 1);
            return puv.Clamp(bounds);
        }


        /// <summary>
        /// �趨��ͼ�߽�
        /// </summary>
        /// <param name="tl"></param>
        /// <param name="br"></param>
        public void SetBounds(PPos tl,PPos br)
        {
            Bounds = EW.OpenGLES.Rectangle.FromLTRB(tl.U, tl.V, br.U + 1, br.V + 1);
            //���ⲻ��Ҫ��ת����ֱ�Ӽ����ͼ��ĻͶ����������絥λ
            var wtop = tl.V * 1024;
            var wbottom = (br.V + 1) * 1024;
            if(Grid.Type == MapGridT.RectangularIsometric)
            {
                wtop /= 2;
                wbottom /= 2;
            }
            else
            {

                ProjectedTopLeft = new WPos(tl.U * 1024, wtop, 0);
                ProjectedBottomRight = new WPos(br.U * 1024 - 1, wbottom - 1, 0);
            }

            ProjectedCellBounds = new ProjectedCellRegion(this, tl, br);
        }

        /// <summary>
        /// �ڵ�ͼ�л���tileset�����ң�����GetTerrainIndex �� GetTerrainInfo ����Ҫÿ���ظ�loop,
        /// ����ռ��������ʱ���50~60%��ռ��CPU������1.3%,����һ����С���ɺ����Ľ��
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public TerrainTypeInfo GetTerrainInfo(CPos cell)
        {
            return Rules.TileSet[GetTerrainIndex(cell)];
        }

        /// <summary>
        /// ��ȡ��������
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public byte GetTerrainIndex(CPos cell)
        {
            const short InvalidCachedTerrainIndex = -1;
            //Lazily initialize a cache for terrain indexes;
            if(cachedTerrainIndexes == null)
            {
                cachedTerrainIndexes = new CellLayer<short>(this);
                cachedTerrainIndexes.Clear(InvalidCachedTerrainIndex);


                //Invalidate the entry for a cell if anything could cause the terrain index to change;
                Action<CPos> invalidateTerrainIndex = c => cachedTerrainIndexes[c] = InvalidCachedTerrainIndex;
                CustomTerrain.CellEntryChanged += invalidateTerrainIndex;
                Tiles.CellEntryChanged += invalidateTerrainIndex;
            }

            var uv = cell.ToMPos(this);

            var terrainIndex = cachedTerrainIndexes[uv];

            if(terrainIndex == InvalidCachedTerrainIndex)
            {
                var custom = CustomTerrain[uv];
                terrainIndex = cachedTerrainIndexes[uv] = custom != byte.MaxValue ? custom : Rules.TileSet.GetTerrainIndex(Tiles[uv]);
            }

            return (byte)terrainIndex;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public Stream Open(string filename)
        {
            if(!filename.Contains("|") && Package.Contains(filename))
            {
                return Package.GetStream(filename);
            }

            return modData.DefaultFileSystem.Open(filename);
        }

        public bool TryGetPackageContaining(string path,out IReadOnlyPackage package,out string filename)
        {
            return modData.DefaultFileSystem.TryGetPackageContaining(path, out package, out filename);
        }

        public bool TryOpen(string filename,out Stream s)
        {
            if(!filename.Contains("|"))
            {
                s = Package.GetStream(filename);
                if (s != null)
                    return true;
            }

            return modData.DefaultFileSystem.TryOpen(filename, out s);
        }

        public bool Exists(string filename)
        {
            if (!filename.Contains("|") && Package.Contains(filename))
                return true;
            return modData.DefaultFileSystem.Exists(filename);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static string ComputeUID(IReadOnlyPackage package)
        {
            var requiredFiles = new[] { "map.yaml", "map.bin" };
            var contents = package.Contents.ToList();

            foreach(var required in requiredFiles)
            {
                if (!contents.Contains(required))
                    throw new FileNotFoundException("Required file {0} not present in this map".F(required));
            }

            using(var ms = new MemoryStream())
            {
                foreach(var filename in contents)
                {
                    if(filename.EndsWith(".yaml") || filename.EndsWith(".bin") || filename.EndsWith(".lua"))
                    {
                        using (var s = package.GetStream(filename))
                            s.CopyTo(ms);
                    }
                }

                ms.Seek(0, SeekOrigin.Begin);
                return CryptoUtil.SHA1Hash(ms);
            }



        }

        /// <summary>
        /// /
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public WDist DistanceAboveTerrain(WPos pos)
        {
            var cell = CellContaining(pos);
            var delta = pos - CenterOfCell(cell);
            return new WDist(delta.Z);
        }

        public int FacingBetween(CPos cell,CPos towards,int fallbackfacing)
        {
            var delta = CenterOfCell(towards) - CenterOfCell(cell);
            if (delta.HorizontalLengthSquared == 0)
                return fallbackfacing;
            return delta.Yaw.Facing;
        }

    }
}