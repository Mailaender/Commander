﻿using System;


namespace EW.Mods.Common.Traits
{


    class ProductionQueueFromSelectionInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new ProductionQueueFromSelection(); }
    }

    class ProductionQueueFromSelection
    {
    }
}