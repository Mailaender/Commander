﻿using System;
using System.Collections.Generic;
using EW.Traits;
using EW.Graphics;
namespace EW.Mods.Common.Traits
{


    /// <summary>
    /// 步兵
    /// </summary>
    public class WithInfantryBodyInfo : ConditionalTraitInfo,Requires<IMoveInfo>,Requires<RenderSpritesInfo>
    {
        public readonly int MinIdleDelay = 30;
        public readonly int MaxIdleDelay = 110;

        [SequenceReference]
        public readonly string MoveSequence = "run";


        /// <summary>
        /// Attack sequence to use for each armament.
        /// </summary>
        [SequenceReference]
        public readonly string DefaultAttackSequence = null;

        public readonly Dictionary<string, string> AttackSequences = new Dictionary<string, string>();

        [SequenceReference]
        public readonly string[] IdleSequences = { };

        [SequenceReference]
        public readonly string[] StandSequences = { "stand" };

        public override object Create(ActorInitializer init)
        {
            return new WithInfantryBody(init, this);
        }
    }
    public class WithInfantryBody:ConditionalTrait<WithInfantryBodyInfo>,ITick,INotifyAttack,INotifyIdle
    {
        enum AnimationState
        {
            Idle,
            Attacking,
            Moving,
            Waiting,
            IdleAnimating
        }
        readonly IMove move;

        protected readonly Animation DefaultAnimation;

        bool dirty;
        AnimationState state;
        int idleDelay;
        string idleSequence;

        IRenderInfantrySequenceModifier rsm;
        bool IsModifyingSequence { get { return rsm != null && rsm.IsModifyingSequence; } }
        bool wasModifying;
        public WithInfantryBody(ActorInitializer init,WithInfantryBodyInfo info) : base(info)
        {
            var self = init.Self;
            var rs = self.Trait<RenderSprites>();

            DefaultAnimation = new Animation(init.World, rs.GetImage(self), RenderSprites.MakeFacingFunc(self));
            rs.Add(new AnimationWithOffset(DefaultAnimation, null, () => IsTraitDisabled));
            PlayStandAnimation(self);

            state = AnimationState.Waiting;
            move = init.Self.Trait<IMove>();
        }

        protected override void Created(Actor self)
        {
            rsm = self.TraitOrDefault<IRenderInfantrySequenceModifier>();
            base.Created(self);
        }


        public virtual void Tick(Actor self)
        {
            if(rsm != null)
            {
                if (wasModifying != rsm.IsModifyingSequence)
                    dirty = true;

                wasModifying = rsm.IsModifyingSequence;
            }

            if((state != AnimationState.Moving || dirty) && move.IsMoving)
            {
                state = AnimationState.Moving;
                DefaultAnimation.PlayRepeating(NormalizeInfantrySequence(self, Info.MoveSequence));
            }
            else if(((state == AnimationState.Moving || dirty) && !move.IsMoving) ||
                    ((state == AnimationState.Idle || state == AnimationState.IdleAnimating) && !self.IsIdle))
            {
                state = AnimationState.Waiting;
                PlayStandAnimation(self);
            }
            dirty = false;
        }

        public void PlayStandAnimation(Actor self)
        {
            var sequence = DefaultAnimation.GetRandomExistingSequence(Info.StandSequences, WarGame.CosmeticRandom);
            if(sequence != null)
            {
                var normalized = NormalizeInfantrySequence(self, sequence);
                DefaultAnimation.PlayRepeating(normalized);
            }
        }


        protected virtual string NormalizeInfantrySequence(Actor self,string baseSequence)
        {
            var prefix = IsModifyingSequence ? rsm.SequencePrefix : "";

            if (DefaultAnimation.HasSequence(prefix + baseSequence))
                return prefix + baseSequence;
            else
            {
                return baseSequence;
            }
        }

        void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
        {
            Attacking(self, target, a);
        }

        void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
        {

        }

        public void Attacking(Actor self,Target target,Armament a)
        {
            string sequence;
            if (!Info.AttackSequences.TryGetValue(a.Info.Name, out sequence))
                sequence = Info.DefaultAttackSequence;

            if(!string.IsNullOrEmpty(sequence) && DefaultAnimation.HasSequence(NormalizeInfantrySequence(self, sequence)))
            {
                state = AnimationState.Attacking;
                DefaultAnimation.PlayThen(NormalizeInfantrySequence(self, sequence), () => state = AnimationState.Idle);
            }
        }


        protected virtual bool AllowIdleAnimation(Actor self)
        {
            return !IsModifyingSequence;
        }

        public void TickIdle(Actor self)
        {
            if(state != AnimationState.Idle && state != AnimationState.IdleAnimating && state != AnimationState.Attacking)
            {
                PlayStandAnimation(self);
                state = AnimationState.Idle;

                if (Info.IdleSequences.Length > 0)
                {
                    idleSequence = Info.IdleSequences.Random(self.World.SharedRandom);
                    idleDelay = self.World.SharedRandom.Next(Info.MinIdleDelay, Info.MaxIdleDelay);
                }
            }
            else if (AllowIdleAnimation(self))
            {
                if(idleSequence != null && DefaultAnimation.HasSequence(idleSequence))
                {
                    if(idleDelay >0 && --idleDelay == 0)
                    {
                        state = AnimationState.IdleAnimating;
                        DefaultAnimation.PlayThen(idleSequence, () =>
                        {
                            PlayStandAnimation(self);
                            state = AnimationState.Waiting;
                        });
                    }
                }
                else
                {
                    PlayStandAnimation(self);
                    state = AnimationState.Waiting;
                }
            }
        }

        
    }
}