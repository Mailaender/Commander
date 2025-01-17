﻿using System;
using EW.Activities;
using EW.Mods.Common.Traits;
using EW.Traits;

namespace EW.Mods.Common.Activities
{
    public class TakeOff:Activity
    {

        readonly Aircraft aircraft;
        readonly IMove move;

        public TakeOff(Actor self)
        {
            aircraft = self.Trait<Aircraft>();
            move = self.Trait<IMove>();

        }


        public override Activity Tick(Actor self)
        {
            //Refuse to take off if it would land immediately again.
            if(aircraft.ForceLanding){
                Cancel(self);
                return NextActivity;
            }

            aircraft.UnReserve();

            var host = aircraft.GetActorBelow();
            var hasHost = host != null;
            var rp = hasHost ? host.TraitOrDefault<RallyPoint>() : null;

            var destination = rp != null ? rp.Location : (hasHost ? self.World.Map.CellContaining(host.CenterPosition) : self.Location);

            if (NextInQueue == null)
                return new AttackMoveActivity(self, move.MoveTo(destination, 1));
            else
                return NextInQueue;
        }
    }
}
