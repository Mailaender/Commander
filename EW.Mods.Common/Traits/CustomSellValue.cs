﻿using System;
using EW.Traits;

namespace EW.Mods.Common.Traits
{

    public class CustomSellValueInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new CustomSellValue(); }
    }
    class CustomSellValue
    {
    }
}