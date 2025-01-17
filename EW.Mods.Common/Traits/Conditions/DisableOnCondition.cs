﻿using System;
using EW.Traits;
namespace EW.Mods.Common.Traits
{
    public class DisableOnConditionInfo : ConditionalTraitInfo
    {
        public override object Create(ActorInitializer init)
        {
            return new DisableOnCondition(this);
        }
    }
    public class DisableOnCondition:ConditionalTrait<DisableOnConditionInfo>
    {

        public DisableOnCondition(DisableOnConditionInfo info) : base(info) { }
    }
}