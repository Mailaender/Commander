﻿using System;
using System.Collections.Generic;
using EW.Traits;

namespace EW.Mods.Common.Traits
{

    public class DemolishableInfo : ITraitInfo
    {
        public object Create(ActorInitializer init)
        {
            return new Demolishable();
        }
    }
    class Demolishable
    {
    }
}