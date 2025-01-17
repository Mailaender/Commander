﻿using System;
using System.Linq;
using EW.Activities;
using EW.Traits;
using EW.Mods.Common.Traits;
using EW.Primitives;

namespace EW.Mods.Common.Activities
{
    public class Transform:Activity
    {
        public readonly string ToActor;
        public CVec Offset = CVec.Zero;
        public int Facing = 96;
        public string[] Sounds = { };
        public string Notification = null;
        public int ForceHealthPercentage = 0;
        public bool SkipMakeAnims = false;
        public string Faction = null;


        public Transform(Actor self, string toActor)
        {
            ToActor = toActor;
        }


        protected override void OnFirstRun(Actor self)
        {
            if (self.Info.HasTraitInfo<IFacingInfo>())
                QueueChild(new Turn(self, Facing));

            if (self.Info.HasTraitInfo<AircraftInfo>())
                QueueChild(new HeliLand(self, true));
        }

        public override Activity Tick(Actor self)
        {

            if (IsCanceled)
                return NextActivity;

            if (ChildActivity != null)
            {
                ActivityUtils.RunActivity(self, ChildActivity);
                return this;
            }


            // Prevent deployment in bogus locations
            var transforms = self.TraitOrDefault<Transforms>();
            var building = self.TraitOrDefault<Building>();
            if ((transforms != null && !transforms.CanDeploy()) || (building != null && !building.Lock()))
            {
                Cancel(self, true);
                return NextActivity;
            }

            foreach (var nt in self.TraitsImplementing<INotifyTransform>())
                nt.BeforeTransform(self);

            var makeAnimation = self.TraitOrDefault<WithMakeAnimation>();
            if(!SkipMakeAnims && makeAnimation != null){

                // Once the make animation starts the activity must not be stopped anymore.
                IsInterruptible = false;

                //wait forever
                QueueChild(new WaitFor(()=> false));
                makeAnimation.Reverse(self,()=>DoTransform(self));
                return this;
            }

            return NextActivity;

        }


        protected override void OnLastRun(Actor self)
        {
            if (!IsCanceled)
                DoTransform(self);
        }


        void DoTransform(Actor self){

            self.World.AddFrameEndTask(w=>{


                if (self.IsDead)
                    return;

                foreach (var nt in self.TraitsImplementing<INotifyTransform>())
                    nt.OnTransform(self);

                var selected = w.Selection.Contains(self);

                var controlgroup = w.Selection.GetControlGroupForActor(self);

                self.Dispose();

                foreach (var s in Sounds)
                    WarGame.Sound.PlayToPlayer(SoundType.World, self.Owner, s, self.CenterPosition);

                WarGame.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Notification, self.Owner.Faction.InternalName);

                var init = new TypeDictionary
                {
                    new LocationInit(self.Location + Offset),
                    new OwnerInit(self.Owner),
                    new FacingInit(Facing),
                };

                if (SkipMakeAnims)
                    init.Add(new SkipMakeAnimsInit());

                if (Faction != null)
                    init.Add(new FactionInit(Faction));

                var health = self.TraitOrDefault<Health>();
                if (health != null)
                {
                    var newHP = ForceHealthPercentage > 0 ? ForceHealthPercentage : (health.HP * 100) / health.MaxHP;
                    init.Add(new HealthInit(newHP));
                }

                var cargo = self.TraitOrDefault<Cargo>();
                if (cargo != null)
                    init.Add(new RuntimeCargoInit(cargo.Passengers.ToArray()));


                var a = w.CreateActor(ToActor, init);
                foreach (var nt in self.TraitsImplementing<INotifyTransform>())
                    nt.AfterTransform(a);

                if (selected)
                    w.Selection.Add(w, a);

                if (controlgroup.HasValue)
                    w.Selection.AddToControlGroup(a, controlgroup.Value);
            });
        }
    }
}
