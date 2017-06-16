﻿
namespace EW
{
    /// <summary>
    /// 
    /// </summary>
    public class PlayerReference
    {
        public string Name;

        public string Faction;  //派系


        public bool LockSpawn = false;
        public int Spawn = 0;

        public bool LockTeam = false;
        public int Team = 0;

        public bool Playable = false;

        public string[] Enemies = { };
        public string[] Allies = { };   //盟国
        public PlayerReference(MiniYaml my)
        {
            FieldLoader.Load(this, my);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}