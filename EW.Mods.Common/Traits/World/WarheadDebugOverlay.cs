﻿using System;


namespace EW.Mods.Common.Traits
{

    public class WarheadDebugOverlayInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new WarheadDebugOverlay(); }
    }
    public class WarheadDebugOverlay
    {
    }
}