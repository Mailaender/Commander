using System;
using System.Collections.Generic;
using System.Linq;
using Eluant;
using Eluant.ObjectBinding;
using EW.Scripting;
using EW.Activities;
using EW.Traits;
using EW.Primitives;
using EW.Graphics;
using EW.OpenGLES;
namespace EW
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Actor:IScriptBindable,IScriptNotifyBind, ILuaTableBinding,ILuaEqualityBinding,IEquatable<Actor>,IDisposable
    {
        /// <summary>
        /// 哈希同步
        /// </summary>
        internal struct SyncHash
        {
            public readonly ISync Trait;
            public readonly int Hash;
            public SyncHash(ISync trait,int hash) { Trait = trait; Hash = hash; }
        }
        public readonly ActorInfo Info;

        public readonly World World;

        public readonly uint ActorID;

        /// <summary>
        /// 占据空间
        /// </summary>
        public IOccupySpace OccupiesSpace { get; private set; }

        public int Generation;
        
        public Rectangle Bounds { get; private set; }

        public Rectangle VisualBounds { get; private set; }

        public ITargetable[] Targetables { get; private set; }
        public bool IsInWorld { get; internal set; }

        public bool IsIdle { get { return CurrentActivity == null; } }

        public bool IsDead { get { return Disposed || (health != null && health.IsDead); } }

        public bool Disposed { get; private set; }

        public IEffectiveOwner EffectiveOwner { get; private set; }
        public Activity CurrentActivity { get; private set; }

        readonly IHealth health;

        readonly IFacing facing;

        readonly IDisable[] disables;

        readonly IRender[] renders;

        readonly IRenderModifier[] renderModifiers;

        readonly IVisibilityModifier[] visibilityModifiers;

        readonly IDefaultVisibility defaultVisibility;

        /// <summary>
        /// Cache sync hash functions per actor for faster sync calculations.(缓存每个Actor 的哈希函数值，用于更快的同步计算
        /// 
        /// 
        /// </summary>
        internal IEnumerable<SyncHash> SyncHashes { get; private set; }
        public Player Owner { get; internal set; }

        public CPos Location { get { return OccupiesSpace.TopLeft; } }

        public WPos CenterPosition { get { return OccupiesSpace.CenterPosition; } }


        public WRot Orientation{

            get{
                
                var facingValue = facing != null ? facing.Facing : 0;
                return new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facingValue));
            }
        }


        internal Actor(World world, string name, TypeDictionary initDict)
        {
            var init = new ActorInitializer(this, initDict);

            World = world;
            ActorID = world.NextAID();

            if (initDict.Contains<OwnerInit>())
                Owner = init.Get<OwnerInit, Player>();
            if (name != null)
            {
                name = name.ToLowerInvariant();

                if (!world.Map.Rules.Actors.ContainsKey(name))
                {
                    throw new NotImplementedException("No rules definition for unit {0}".F(name));
                }
                Info = world.Map.Rules.Actors[name];
                foreach (var trait in Info.TraitsInConstructOrder())
                {
                    AddTrait(trait.Create(init));
                    if (trait is IOccupySpaceInfo)
                    {
                        OccupiesSpace = Trait<IOccupySpace>();
                    }
                }
            }

            EffectiveOwner = TraitOrDefault<IEffectiveOwner>();

            facing = TraitOrDefault<IFacing>();
            health = TraitOrDefault<IHealth>();
            disables = TraitsImplementing<IDisable>().ToArray();
            renders = TraitsImplementing<IRender>().ToArray();
            renderModifiers = TraitsImplementing<IRenderModifier>().ToArray();
            visibilityModifiers = TraitsImplementing<IVisibilityModifier>().ToArray();
            defaultVisibility = Trait<IDefaultVisibility>();
            Targetables = TraitsImplementing<ITargetable>().ToArray();
            Bounds = DetermineBounds();

            SyncHashes = TraitsImplementing<ISync>()
                .Select(sync => Pair.New(sync, Sync.GetHashFunction(sync)))
                .ToArray()
                .Select(pair => new SyncHash(pair.First, pair.Second(pair.First)));
        }





        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Rectangle DetermineBounds()
        {
            var si = Info.TraitInfoOrDefault<SelectableInfo>();
            var size = (si != null && si.Bounds != null) ? new Int2(si.Bounds[0], si.Bounds[1]) : TraitsImplementing<IAutoSelectionSize>().Select(x => x.SelectionSize(this)).FirstOrDefault();

            var offset = -size / 2;
            if (si != null && si.Bounds != null && si.Bounds.Length > 2)
                offset += new Int2(si.Bounds[2], si.Bounds[3]);

            return new Rectangle(offset.X, offset.Y, size.X, size.Y);
        }
        

        public void Tick()
        {
            var wasIdle = IsIdle;
            CurrentActivity = ActivityUtils.RunActivity(this, CurrentActivity);

            if(!wasIdle && IsIdle)
            {
                foreach (var n in TraitsImplementing<INotifyBecomingIdle>())
                    n.OnBecomingIdle(this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wr"></param>
        /// <returns></returns>
        public IEnumerable<IRenderable> Render(WorldRenderer wr)
        {
            //PERF:Avoid LINQ;
            var renderables = Renderables(wr);
            foreach (var modifier in renderModifiers)
                renderables = modifier.ModifyRender(this, wr, renderables);

            return renderables;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="wr"></param>
        /// <returns></returns>
        IEnumerable<IRenderable> Renderables(WorldRenderer wr)
        {

            //PERF:Avoid LINQ.
            //Implementations of Render are permitted to return both an eagerly materialized collection or a lazily generated sequence.
            //For large amounts of renderables,a lazily generated sequence(e.g. as returned by LINQ,or by using 'yield') will avoid the need to allocate a large collection.
            //For small amounts of renderables, allocating a small collection can often be faster and require less memory than creating the objects needed to represent a sequence.
            foreach (var render in renders)
                foreach (var renderable in render.Render(this, wr))
                    yield return renderable;
        }


        public bool Equals(Actor other)
        {
            return ActorID == other.ActorID;
        }

        /// <summary>
        /// 可视性
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool CanBeViewedByPlayer(Player player)
        {
            foreach (var visibilityModifier in visibilityModifiers)
                if (!visibilityModifier.IsVisible(this, player))
                    return false;
            return defaultVisibility.IsVisible(this, player);
        }

        public IEnumerable<string> GetAllTargetTypes()
        {
            //PERF:Avoid LINQ;

            foreach (var targetable in Targetables)
                foreach (var targetType in targetable.TargetTypes)
                    yield return targetType;
        }

        public IEnumerable<string> GetEnabledTargetTypes()
        {
            //PERF:Avoid LINQ;
            foreach (var targetable in Targetables)
                if (targetable.IsTraitEnabled())
                    foreach (var targetType in targetable.TargetTypes)
                        yield return targetType;
        }

        public bool IsDisabled()
        {
            foreach (var disable in disables)
                if (disable.Disabled)
                    return true;
            return false;
        }

        public DamageState GetDamageState()
        {
            if (Disposed)
                return DamageState.Dead;
            return (health == null) ? DamageState.Undamaged : health.DamageState;
        }

        #region Trait

        /// <summary>
        /// 
        /// </summary>
        /// <param name="trait"></param>
        public void AddTrait(object trait)
        {
            World.TraitDict.AddTrait(this, trait);
        }
        public T TraitOrDefault<T>()
        {
            return World.TraitDict.GetOrDefault<T>(this);
        }

        public T Trait<T>()
        {
            return World.TraitDict.Get<T>(this);
        }

        public IEnumerable<T> TraitsImplementing<T>()
        {
            return World.TraitDict.WithInterface<T>(this);
        }
        #endregion


        #region Scripting interface


        Lazy<ScriptActorInterface> luaInterface;
        public void OnScriptBind(ScriptContext context)
        {
            if (luaInterface == null)
                luaInterface = Exts.Lazy(() => new ScriptActorInterface(context, this));
        }

        public LuaValue this[LuaRuntime runtime,LuaValue keyValue]
        {
            get { return luaInterface.Value[runtime, keyValue]; }
            set { luaInterface.Value[runtime, keyValue] = value; }
        }

        public LuaValue Equals(LuaRuntime runtime,LuaValue left,LuaValue right)
        {
            Actor a, b;
            if(!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
            {
                return false;
            }
            return a == b;
        }

        public LuaValue ToString(LuaRuntime runtime)
        {
            //PERF:Avoid format strings
            //return "Actor ({0})".F(this);
            var name = Info.Name + "    " + ActorID;
            if (!IsInWorld)
                name += "( not in world)";
            return name;
        }

        public bool HasScriptProperty(string name)
        {
            return luaInterface.Value.ContainsKey(name);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="newOwner"></param>
        public void ChangeOwner(Player newOwner)
        {

        }

        #endregion


        #region Activity

        public void QueueActivity(bool queued,Activity nextActivity)
        {
            if (!queued)
                CancelActivity();
            QueueActivity(nextActivity);
        }

        public void QueueActivity(Activity nextActivity)
        {
            if (CurrentActivity == null)
                CurrentActivity = nextActivity;
            else
                CurrentActivity.Queue(nextActivity);



        }


        public bool CancelActivity()
        {
            //if (currentActivity != null)
            //    currentActivity.Cancel(this);

            if (CurrentActivity != null)
                return CurrentActivity.RootActivity.Cancel(this);

            return true;
        }

        #endregion
        public void Dispose()
        {
            World.AddFrameEndTask(w =>
            {
                if (Disposed)
                    return;

                if (IsInWorld)
                    World.Remove(this);

                foreach (var t in TraitsImplementing<INotifyActorDisposing>())
                    t.Disposing(this);

                World.TraitDict.RemoveActor(this);
                Disposed = true;

                if (luaInterface != null)
                    luaInterface.Value.OnActorDestroyed();
            });
        }

    }
}