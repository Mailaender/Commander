﻿using System;
using System.Collections.Generic;
using System.Drawing;
using EW.Traits;
using EW.Graphics;
using EW.Mods.Common.Graphics;
namespace EW.Mods.Common.Traits
{
    /// <summary>
    /// Default trait for rendering sprite-based actors.
    /// </summary>
    public class WithSpriteBodyInfo : PausableConditionalTraitInfo,IRenderActorPreviewSpritesInfo,Requires<RenderSpritesInfo>
    {

        /// <summary>
        /// Animation to play when the actor is created.
        /// </summary>
        [SequenceReference]
        public readonly string StartSequence = null;

        /// <summary>
        /// Animation to play when the actor is idle.
        /// </summary>
        [SequenceReference]
        public readonly string Sequence = "idle";

        /// <summary>
        /// Pause animation when actor is disabled.
        /// </summary>
        public readonly bool PauseAnimationWhenDisabled = false;

        /// <summary>
        /// Identifier used to assign modifying traits to this sprite body.
        /// </summary>
        public readonly string Name = "body";
        public override object Create(ActorInitializer init)
        {
            return new WithSpriteBody(init, this);
        }

        public virtual IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init,RenderSpritesInfo rs,string image,int facings,PaletteReference p)
        {
            if (!EnabledByDefault)
                yield break;

            var anim = new Animation(init.World, image);
            anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

            yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p, rs.Scale);
        }
    }

    public class WithSpriteBody:PausableConditionalTrait<WithSpriteBodyInfo>, INotifyBuildComplete,INotifyDamageStateChanged,IAutoMouseBounds
    {

        public readonly Animation DefaultAnimation;
        readonly RenderSprites rs;
        public WithSpriteBody(ActorInitializer init,WithSpriteBodyInfo info) : this(init, info, () => 0) { }

        protected WithSpriteBody(ActorInitializer init,WithSpriteBodyInfo info,Func<int> baseFacing) : base(info)
        {
            rs= init.Self.Trait<RenderSprites>();

            Func<bool> paused = null;
            if (info.PauseAnimationWhenDisabled)
                paused = () => init.Self.IsDisabled() && DefaultAnimation.CurrentSequence.Name == NormalizeSequence(init.Self, Info.Sequence);

            DefaultAnimation = new Animation(init.World, rs.GetImage(init.Self), baseFacing, paused);
            rs.Add(new AnimationWithOffset(DefaultAnimation, null, () => IsTraitDisabled));

            if(info.StartSequence !=null)
            {
                PlayCustomAnimation(init.Self, info.StartSequence, () => PlayCustomAnimationRepeating(init.Self, info.Sequence));
            }
            else
            {
                DefaultAnimation.PlayRepeating(NormalizeSequence(init.Self, info.Sequence));
            }
        }


        //TODO:Get rid of INotifyBuildComplete in favor of using the condition system.
        //摆脱INotifyBuildComplete有利于使用条件系统
        void INotifyBuildComplete.BuildingComplete(Actor self)
        {
            OnBuildComplete(self);
        }

        protected virtual void OnBuildComplete(Actor self)
        {
            DefaultAnimation.PlayRepeating(NormalizeSequence(self, Info.Sequence));
        }
        public string NormalizeSequence(Actor self,string sequence)
        {
            return RenderSprites.NormalizeSequence(DefaultAnimation, self.GetDamageState(), sequence);
        }

        public void PlayCustomAnimation(Actor self,string name,Action after = null)
        {
            DefaultAnimation.PlayThen(NormalizeSequence(self, name), () =>
            {
                DefaultAnimation.Play(NormalizeSequence(self, Info.Sequence));
                if (after != null)
                    after();
            });
        }


        public void CancelCustomAnimation(Actor self)
        {
            DefaultAnimation.PlayRepeating(NormalizeSequence(self, Info.Sequence));
        }

        public void PlayCustomAnimationRepeating(Actor self,string name)
        {
            var sequence = NormalizeSequence(self, name);
            DefaultAnimation.PlayThen(sequence, () => PlayCustomAnimationRepeating(self, sequence));
        }


        public void PlayCustomAnimationBackwards(Actor self,string name,Action after = null)
        {
            DefaultAnimation.PlayBackwardsThen(NormalizeSequence(self, name), () =>
            {
                CancelCustomAnimation(self);
                if (after != null)
                    after();
            });
        }

        void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo attackInfo)
        {
            DamageStateChanged(self);
        }

        protected virtual void DamageStateChanged(Actor self)
        {
            if (DefaultAnimation.CurrentSequence != null)
                DefaultAnimation.ReplaceAnim(NormalizeSequence(self, DefaultAnimation.CurrentSequence.Name));
        }


        Rectangle IAutoMouseBounds.AutoMouseoverBounds(Actor self, WorldRenderer wr)
        {
            return DefaultAnimation != null ? DefaultAnimation.ScreenBounds(wr, self.CenterPosition, WVec.Zero, rs.Info.Scale) : Rectangle.Empty;
        }
    }
}