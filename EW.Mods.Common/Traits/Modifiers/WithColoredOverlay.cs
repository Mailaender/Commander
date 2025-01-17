﻿using System;
using System.Collections.Generic;
using EW.Traits;
using EW.Graphics;
namespace EW.Mods.Common.Traits
{


    public class WithColoredOverlayInfo : ConditionalTraitInfo
    {
        public override object Create(ActorInitializer init)
        {
            return new WithColoredOverlay(this);
        }
    }
    public class WithColoredOverlay : ConditionalTrait<WithColoredOverlayInfo>
    {

        public WithColoredOverlay(WithColoredOverlayInfo info) : base(info) { }
    }
}