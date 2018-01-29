﻿using System;
using System.Collections.Generic;

namespace EW.Traits
{
    public class ShroudInfo:ITraitInfo
    {
        public readonly string FogCheckboxLabel = "Fog of war";

        public readonly bool FogCheckboxEnabled = true;

        public readonly bool FogCheckboxLocked = false;

        public readonly bool ExploredMapCheckboxEnabled = false;



        public object Create(ActorInitializer init) { return new Shroud(init.Self,this); }
    }


    public class Shroud:ISync,INotifyCreated
    {
        public enum SourceType : byte { PassiveVisibility,Shroud,Visibility}

        class ShroudSource
        {
            public readonly SourceType Type;
            public readonly PPos[] ProjectedCells;

            public ShroudSource(SourceType type,PPos[] projectedCells)
            {
                Type = type;
                ProjectedCells = projectedCells;
            }
        }

        enum ShroudCellType : byte { Shroud,Fog,Visible}
        readonly Actor self;
        readonly ShroudInfo info;
        readonly Map map;

        //Individual shroud modifier sources(type and area)
        readonly Dictionary<object, ShroudSource> sources = new Dictionary<object, ShroudSource>();

        //Per-cell count of each source type,used to resolve the final cell type
        readonly CellLayer<short> passiveVisibleCount;
        readonly CellLayer<short> visibleCount;
        readonly CellLayer<short> generatedShroudCount;
        readonly CellLayer<bool> explored;

        //Per-cell cache of the resolved cell type (shroud/fog/visible)
        readonly CellLayer<ShroudCellType> resolvedType;

        [Sync]
        bool disabled;

        public event Action<IEnumerable<PPos>> CellsChanged;

        public bool Disabled
        {
            get { return disabled; }
            set
            {
                if (disabled == value)
                    return;
                disabled = value;

            }
        }

        bool fogEnabled;

        public bool FogEnabled { get { return !Disabled && fogEnabled; } }

        public bool ExploreMapEnabled { get; private set; }


        public int Hash { get; private set; }

        //Enabled at runtime on first use
        bool shroudGenerationEnabled;
        bool passiveVisibilityEnabled;

        public Shroud(Actor self,ShroudInfo info)
        {
            this.self = self;
            this.info = info;
            map = this.self.World.Map;

            passiveVisibleCount = new CellLayer<short>(map);
            visibleCount = new CellLayer<short>(map);
            generatedShroudCount = new CellLayer<short>(map);
            explored = new CellLayer<bool>(map);

            resolvedType = new CellLayer<ShroudCellType>(map);
        }


        void INotifyCreated.Created(Actor self)
        {
            var gs = self.World.LobbyInfo.GlobalSettings;
            fogEnabled = gs.OptionOrDefault("fog", info.FogCheckboxEnabled);

            ExploreMapEnabled = gs.OptionOrDefault("explored", info.ExploredMapCheckboxEnabled);

            //if (ExploreMapEnabled)
                self.World.AddFrameEndTask(w => ExploreAll());
        }

        public void ExploreAll()
        {
            var changed = new List<PPos>();
            foreach(var puv in map.ProjectedCellBounds)
            {
                var uv = (MPos)puv;
                if (!explored[uv])
                {
                    explored[uv] = true;
                    changed.Add(puv);
                }
            }

            Invalidate(changed);
        }


        void Invalidate(IEnumerable<PPos> changed)
        {
            foreach(var puv in changed)
            {
                var uv = (MPos)puv;
                var type = ShroudCellType.Shroud;

                if(explored[uv] && (!shroudGenerationEnabled || generatedShroudCount[uv] == 0 || visibleCount[uv] == 0))
                {
                    var count = visibleCount[uv];
                    if (passiveVisibilityEnabled)
                        count += passiveVisibleCount[uv];

                    type = count > 0 ? ShroudCellType.Visible : ShroudCellType.Fog
;                }

                resolvedType[uv] = type;
            }

            if (CellsChanged != null)
                CellsChanged(changed);

            var oldHash = Hash;
            Hash = Sync.HashPlayer(self.Owner) + self.World.WorldTick * 3;

            //Invalidate may be called multiple times in one world tick,which is decoupled from rendering
            //无效可能会在同一个世界中被多次调用，与渲染分离。
            if (oldHash == Hash)
                Hash += 1;
        }


        public void AddSource(object key,SourceType type,PPos[] projectedCells)
        {
            if (sources.ContainsKey(key))
                throw new InvalidOperationException("Attempting to add duplicate shroud source");

            sources[key] = new ShroudSource(type, projectedCells);

            foreach(var puv in projectedCells)
            {
                //Force cells outside the visible bounds invisible
                if (!map.Contains(puv))
                    continue;

                var uv = (MPos)puv;
                switch (type)
                {
                    case SourceType.PassiveVisibility:
                        passiveVisibilityEnabled = true;
                        passiveVisibleCount[uv]++;
                        explored[uv] = true;
                        break;
                    case SourceType.Visibility:
                        visibleCount[uv]++;
                        explored[uv] = true;
                        break;
                    case SourceType.Shroud:
                        shroudGenerationEnabled = true;
                        generatedShroudCount[uv]++;
                        break;
                }
            }
        }


        public void RemoveSource(object key)
        {
            ShroudSource state;
            if (!sources.TryGetValue(key, out state))
                return;

            foreach(var puv in state.ProjectedCells)
            {
                if (map.Contains(puv))
                {
                    var uv = (MPos)puv;
                    switch (state.Type)
                    {
                        case SourceType.PassiveVisibility:
                            passiveVisibleCount[uv]--;
                            break;
                        case SourceType.Visibility:
                            visibleCount[uv]--;
                            break;
                        case SourceType.Shroud:
                            generatedShroudCount[uv]--;
                            break;
                    }
                }
            }
            sources.Remove(key);
        }

        public bool IsVisible(CPos cell)
        {
            return IsVisible(cell.ToMPos(map));
        }

        public bool IsVisible(MPos uv)
        {
            if (!resolvedType.Contains(uv))
                return false;

            foreach (var puv in map.ProjectedCellsCovering(uv))
                if (IsVisible(puv))
                    return true;

            return false;
        }

        public bool IsVisible(WPos pos)
        {
            return IsVisible(map.ProjectedCellCovering(pos));
        }

        public bool IsVisible(PPos puv)
        {
            if (!FogEnabled)
                return map.Contains(puv);

            var uv = (MPos)puv;
            return resolvedType.Contains(uv) && resolvedType[uv] == ShroudCellType.Visible;
        }


        public bool IsExplored(PPos puv)
        {
            if (Disabled)
                return map.Contains(puv);

            var uv = (MPos)puv;
            return resolvedType.Contains(uv) && resolvedType[uv] > ShroudCellType.Shroud;
        }


        public static IEnumerable<PPos> ProjectedCellsInRange(Map map,CPos cell,WDist range,int maxHeightDelta = -1)
        {
            return ProjectedCellsInRange(map, map.CenterOfCell(cell), range, maxHeightDelta);
        }

        public static IEnumerable<PPos> ProjectedCellsInRange(Map map,WPos pos,WDist range,int maxHeightDelta = -1)
        {
            var r = (range.Length + 1023 + 512) / 1024;
            var limit = range.LengthSquared;


            //Project actor position into the shroud plane
            var projectedPos = pos - new WVec(0, pos.Z, pos.Z);
            var projectedCell = map.CellContaining(projectedPos);
            var projectedHeight = pos.Z / 512;

            foreach(var c in map.FindTilesInCircle(projectedCell, r, true))
            {
                if((map.CenterOfCell(c) - projectedPos).HorizontalLengthSquared<= limit)
                {
                    var puv = (PPos)c.ToMPos(map);
                    if (maxHeightDelta < 0 || map.ProjectedHeight(puv) < projectedHeight + maxHeightDelta)
                        yield return puv;
                }
            }

        }
    }
}