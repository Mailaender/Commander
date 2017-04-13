using System;
using System.Collections.Generic;


namespace EW
{

    public class ModMetadata
    {
        public string Title;
        public string Description;
        public string Version;
        public string Author;
        public bool Hidden;

    }
    /// <summary>
    /// ����һ��Mode��Ҫ���ص������嵥
    /// </summary>
    public class Manifest
    {

        public readonly string[] Rules, ServerTraits, Sequences,VoxelSequences,Cursors,Chrome,Assemblies,ChromeLayout,Weapons,Voices,Notifications,Music,Translations,TileSets,Missions;


    }
}