﻿using System;
using System.Collections.Generic;
using EW.Traits;

namespace EW.Mods.Common.Traits
{
    public class RevealsShroudInfo : AffectsShroudInfo
    {
        public readonly Stance ValidStances = Stance.Ally;

        public readonly bool RevealGeneratedShroud = true;

        public override object Create(ActorInitializer init)
        {
            return new RevealsShroud(init.Self, this);
        }
    }
    public class RevealsShroud:AffectsShroud
    {

        readonly RevealsShroudInfo info;
        readonly Shroud.SourceType type;
        public RevealsShroud(Actor self,RevealsShroudInfo info) : base(self, info)
        {
            this.info = info;
            type = info.RevealGeneratedShroud ? Shroud.SourceType.Visibility : Shroud.SourceType.PassiveVisibility;
        }



        protected override void AddCellsToPlayerShroud(Actor self, Player player, PPos[] uv)
        {
            if (!info.ValidStances.HasStance(player.Stances[self.Owner]))
                return;
            player.Shroud.AddSource(this, type, uv);
        }

        protected override void RemoveCellsFromPlayerShroud(Actor self, Player player)
        {
            player.Shroud.RemoveSource(this);
        }
    }
}