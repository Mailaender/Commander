﻿using System;
using System.Collections.Generic;
using EW.Traits;
namespace EW.Mods.Common.Traits
{

    public class BibInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new Bib(); }
    }
    class Bib
    {
    }
}