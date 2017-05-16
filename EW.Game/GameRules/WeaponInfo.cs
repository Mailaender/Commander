using System;
using System.Collections.Generic;
using System.Linq;
using EW.Traits;
using EW.Effects;
namespace EW
{

    public class ProjectileArgs
    {
        public WeaponInfo Weapon;
        public int[] DamagedModifiers;

        /// <summary>
        /// ����ȷ
        /// </summary>
        public int[] InaccuracyModifiers;

        public int[] RangeModifiers;

        public int Facing;

        public WPos Source;

        public Func<WPos> CurrentSource;

        public Actor SourceActor;
        /// <summary>
        /// ����Ŀ��
        /// </summary>
        public WPos PassiveTarget;

        /// <summary>
        /// ָ��Ŀ��
        /// </summary>
        public Target GuidedTarget;
    }

    public interface IProjectile:IEffect { }
    public interface IProjectileInfo { IProjectile Create(ProjectileArgs args); }
    public sealed class WeaponInfo
    {
        /// <summary>
        /// ����������������Χ
        /// </summary>
        public readonly WDist Range = WDist.Zero;

        public readonly WDist MinRange = WDist.Zero;

        /// <summary>
        /// The sound played when the weapon is fired.
        /// </summary>
        public readonly string[] Report = null;

        /// <summary>
        /// ����װ�ص�ҩ֮����ӳ�
        /// </summary>
        public readonly int ReloadDelay = 1;

        /// <summary>
        /// 
        /// </summary>
        public readonly HashSet<string> ValidTargets = new HashSet<string> { "Gound", "Water" };

        /// <summary>
        /// 
        /// </summary>
        public readonly HashSet<string> InvalidTargets = new HashSet<string>();

        public readonly int Burst = 1;

        public readonly int BurstDelay = 5;

        [FieldLoader.LoadUsing("LoadProjectile")]
        public readonly IProjectileInfo Projectile;

        [FieldLoader.LoadUsing("LoadWarheads")]
        public readonly List<IWarHead> Warheads = new List<IWarHead>();

        public WeaponInfo(string name, MiniYaml content)
        {
            FieldLoader.Load(this, content);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaml"></param>
        /// <returns></returns>
        static object LoadProjectile(MiniYaml yaml)
        {
            MiniYaml proj;
            if (!yaml.ToDictionary().TryGetValue("Projectile", out proj))
                return null;

            var ret = WarGame.CreateObject<IProjectileInfo>(proj.Value + "Info");
            FieldLoader.Load(ret, proj);
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yaml"></param>
        /// <returns></returns>
        static object LoadWarheads(MiniYaml yaml)
        {
            var retList = new List<IWarHead>();

            foreach(var node in yaml.Nodes.Where(n => n.Key.StartsWith("Warhead")))
            {
                var ret = WarGame.CreateObject<IWarHead>(node.Value.Value + "Warhead");
                FieldLoader.Load(ret, node.Value);
                retList.Add(ret);
            }

            return retList;
        }

        public void Impact(Target target,Actor firedBy,IEnumerable<int> damageModifiers)
        {

        }


    }
}