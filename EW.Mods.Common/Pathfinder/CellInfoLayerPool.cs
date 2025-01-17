﻿using System;
using System.Drawing;
using System.Collections.Generic;

namespace EW.Mods.Common.Pathfinder
{
    /// <summary>
    /// 
    /// </summary>
    sealed class CellInfoLayerPool
    {
        const int MaxPoolSize = 4;
        readonly Stack<CellLayer<CellInfo>> pool = new Stack<CellLayer<CellInfo>>(MaxPoolSize);
        readonly CellLayer<CellInfo> defaultLayer;

        public CellInfoLayerPool(Map map)
        {
            defaultLayer = CellLayer<CellInfo>.CreateInstance(mpos =>
            new CellInfo(int.MaxValue, int.MaxValue, mpos.ToCPos(map), CellStatus.Unvisited),
            new Size(map.MapSize.X, map.MapSize.Y), map.Grid.Type);
        }
        public PooledCellInfoLayer Get()
        {
            return new PooledCellInfoLayer(this);
        }

        CellLayer<CellInfo> GetLayer()
        {
            CellLayer<CellInfo> layer = null;
            lock (pool)
                if (pool.Count > 0)
                    layer = pool.Pop();

            if (layer == null)
                layer = new CellLayer<CellInfo>(defaultLayer.GridT, defaultLayer.Size);
            layer.CopyValuesFrom(defaultLayer);
            return layer;
        }

        void ReturnLayer(CellLayer<CellInfo> layer)
        {
            lock (pool)
                if (pool.Count < MaxPoolSize)
                    pool.Push(layer);
        }

        public class PooledCellInfoLayer : IDisposable
        {
            //public CellLayer<CellInfo> Layer { get; private set; }
            List<CellLayer<CellInfo>> layers = new List<CellLayer<CellInfo>>();
            CellInfoLayerPool layerPool;

            public PooledCellInfoLayer(CellInfoLayerPool layerPool)
            {
                this.layerPool = layerPool;
            }


            public CellLayer<CellInfo> GetLayer(){

                var layer = layerPool.GetLayer();
                layers.Add(layer);
                return layer;

            }
               
            public void Dispose()
            {

                if(layerPool!=null){
                    foreach (var layer in layers)
                        layerPool.ReturnLayer(layer);
                }
                layers = null;
                layerPool = null;
            }
        }

    }
}