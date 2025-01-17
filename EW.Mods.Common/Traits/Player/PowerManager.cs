﻿using System;
using System.Collections.Generic;
using System.Linq;
using EW.Traits;
namespace EW.Mods.Common.Traits
{
    public class PowerManagerInfo : ITraitInfo,Requires<DeveloperModeInfo>
    {

        public readonly int AdviceInterval = 250;

        public readonly string SpeechNotification = "LowPower";


        public object Create(ActorInitializer init) { return new PowerManager(init.Self,this); }
    }
    public class PowerManager:INotifyCreated,ITick,ISync,IResolveOrder
    {
        readonly Actor self;
        readonly PowerManagerInfo info;
        readonly DeveloperMode devMode;

        readonly Dictionary<Actor, int> powerDrain = new Dictionary<Actor, int>();

        [Sync] int totalProvided;

        public int PowerProvided { get { return totalProvided; } }

        [Sync] int totalDrained;

        public int PowerDrained { get { return totalProvided; } }

        public int ExcessPower { get { return totalProvided - totalDrained; } }

        public PowerState PowerState
        {
            get
            {
                if (PowerProvided >= PowerDrained) return PowerState.Normal;

                if (PowerProvided > PowerDrained / 2) return PowerState.Low;

                return PowerState.Critical;
            }
        }

        public int PowerOutageRemainingTicks { get; private set; }
        public int PowerOutageTotalTicks { get; private set; }


        int nextPowerAdviceTime = 0;
        bool isLowPower = false;
        bool wasLowPower = false;
        bool wasHackEnabled;

        public PowerManager(Actor self,PowerManagerInfo info)
        {
            this.self = self;
            this.info = info;

            devMode = self.Trait<DeveloperMode>();
            wasHackEnabled = devMode.UnlimitedPower;


            
        }

        void INotifyCreated.Created(Actor self){

            // Map placed actors will query an inconsistent power state when they are created
            // (it will depend on the order that they are spawned by the world)
            // Tell them to query the correct state once the world has been fully created
            self.World.AddFrameEndTask(w=>UpdatePowerRequiringActors());
        }

        void ITick.Tick(Actor self)
        {

            if (wasHackEnabled != devMode.UnlimitedPower)
            {
                totalProvided = 0;
                totalDrained = 0;

                if (!devMode.UnlimitedPower)
                    foreach (var kv in powerDrain)
                        if (kv.Value > 0)
                            totalProvided += kv.Value;
                        else if (kv.Value < 0)
                            totalDrained -= kv.Value;

                wasHackEnabled = devMode.UnlimitedPower;
            }

            isLowPower = ExcessPower < 0;

            if (isLowPower != wasLowPower)
                UpdatePowerRequiringActors();

            if (isLowPower && !wasLowPower)
                nextPowerAdviceTime = 0;

            wasLowPower = isLowPower;

            if (--nextPowerAdviceTime <= 0)
            {
                if (isLowPower)
                    WarGame.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.SpeechNotification, self.Owner.Faction.InternalName);

                nextPowerAdviceTime = info.AdviceInterval;
            }

            if (PowerOutageRemainingTicks > 0 && --PowerOutageRemainingTicks == 0)
                UpdatePowerOutageActors();
        }


        void UpdatePowerRequiringActors()
        {
            var traitPairs = self.World.ActorsWithTrait<INotifyPowerLevelChanged>()
                .Where(p => !p.Actor.IsDead && p.Actor.IsInWorld && p.Actor.Owner == self.Owner);

            foreach (var p in traitPairs)
                p.Trait.PowerLevelChanged(p.Actor);
        }

        public void UpdateActor(Actor a)
        {
            int old;
            powerDrain.TryGetValue(a, out old); // old is 0 if a is not in powerDrain
            var amount = a.TraitsImplementing<Power>().Where(t => !t.IsTraitDisabled).Aggregate(0, (v, p) => v + p.GetEnabledPower());
            powerDrain[a] = amount;
            if (amount == old || devMode.UnlimitedPower)
                return;
            if (old > 0)
                totalProvided -= old;
            else if (old < 0)
                totalDrained += old;
            if (amount > 0)
                totalProvided += amount;
            else if (amount < 0)
                totalDrained -= amount;

        }

        void IResolveOrder.ResolveOrder(Actor self, NetWork.Order order)
        {
            if(devMode.Enabled && order.OrderString == "PowerOutage")
            {
                TriggerPowerOutage((int)order.ExtraData);
            }
        }

        public void TriggerPowerOutage(int totalTicks)
        {
            PowerOutageTotalTicks = PowerOutageRemainingTicks = totalTicks;
            UpdatePowerOutageActors();
        }

        void UpdatePowerOutageActors()
        {
            var traitPairs = self.World.ActorsWithTrait<AffectedByPowerOutage>()
                .Where(p => !p.Actor.IsDead && p.Actor.IsInWorld && p.Actor.Owner == self.Owner);

            foreach (var p in traitPairs)
                p.Trait.UpdateStatus(p.Actor);
        }

        public void RemoveActor(Actor a)
        {
            int amount;
            if (!powerDrain.TryGetValue(a, out amount))
                return;
            powerDrain.Remove(a);

            if (devMode.UnlimitedPower)
                return;

            if (amount > 0)
                totalProvided -= amount;
            else if (amount < 0)
                totalDrained += amount;
        }
    }
}