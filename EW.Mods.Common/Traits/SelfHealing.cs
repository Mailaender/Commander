﻿using System;
using System.Collections.Generic;
using EW.Traits;

namespace EW.Mods.Common.Traits
{
    /// <summary>
    /// Attach this to actors which should be able to regenerate their health points.
    /// </summary>
    class SelfHealingInfo : ConditionalTraitInfo,Requires<HealthInfo>
    {

        [Desc("Absolute amount of health points added in each step.")]
        public readonly int Step = 5;

        [Desc("Relative percentages of health added in each step.",
            "When both values are defined, their summary will be applied.")]
        public readonly int PercentageStep = 0;

        public readonly int Delay = 5;

        [Desc("Heal if current health is below this percentage of full health.")]
        public readonly int HealIfBelow = 50;

        public readonly int DamageCooldown = 0;

        [Desc("Apply the selfhealing using these damagetypes.")]
        public readonly HashSet<string> DamageTypes = new HashSet<string>();

        public override object Create(ActorInitializer init)
        {
            return new SelfHealing(init.Self, this);
        }
    }
    class SelfHealing:ConditionalTrait<SelfHealingInfo>,ITick,INotifyDamage
    {
        readonly Health health;

        [Sync] int ticks;
        [Sync] int damageTicks;

        public SelfHealing(Actor self, SelfHealingInfo info)
            : base(info)
        {
            health = self.Trait<Health>();
        }

        void ITick.Tick(Actor self)
        {
            if (self.IsDead || IsTraitDisabled)
                return;

            if (health.HP >= Info.HealIfBelow * health.MaxHP / 100)
                return;

            if (damageTicks > 0)
            {
                --damageTicks;
                return;
            }

            if (--ticks <= 0)
            {
                ticks = Info.Delay;
                self.InflictDamage(self, new Damage(-(Info.Step + Info.PercentageStep * health.MaxHP / 100), Info.DamageTypes));
            }
        }

        public void Damaged(Actor self, AttackInfo e)
        {
            if (e.Damage.Value > 0)
                damageTicks = Info.DamageCooldown;
        }
    }
}