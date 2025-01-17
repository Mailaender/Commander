﻿using System;
using System.Linq;
namespace EW.Mods.Common.Traits
{
    public static class BuildingUtils
    {
        public static bool IsCellBuildable(this World world,CPos cell,BuildingInfo bi,Actor toIgnore = null){

            if (!world.Map.Contains(cell))
                return false;

            if (world.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(cell) != null)
                return false;

            if(!bi.AllowInvalidPlacement && world.ActorMap.GetActorsAt(cell).Any(a=>a!=toIgnore)){
                return false;
            }

            var tile = world.Map.Tiles[cell];
            var tileInfo = world.Map.Rules.TileSet.GetTileInfo(tile);

            if (tileInfo != null && tileInfo.RampType > 0)
                return false;
            return bi.TerrainTypes.Contains(world.Map.GetTerrainInfo(cell).Type);
        }




        public static bool CanPlaceBuilding(this World world,string name,BuildingInfo building,CPos topLeft,Actor toIgnore)
        {
            if (building.AllowInvalidPlacement)
                return true;

            var res = world.WorldActor.TraitOrDefault<ResourceLayer>();
            return building.Tiles(topLeft).All(t => world.Map.Contains(t) 
            && (res == null || res.GetResource(t) == null) 
            && world.IsCellBuildable(t, building, toIgnore));
        }
    }
}
