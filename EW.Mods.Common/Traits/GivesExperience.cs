﻿using System;


namespace EW.Mods.Common.Traits
{

    class GivesExperienceInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new GivesExperience(); }
    }
    class GivesExperience
    {
    }
}