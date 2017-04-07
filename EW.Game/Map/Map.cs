using System;
using System.Collections.Generic;
using System.IO;
using EW.FileSystem;
namespace EW
{
    /// <summary>
    /// 
    /// </summary>
    public class Map:IReadOnlyFileSystem
    {
        readonly ModData modData;
        public Stream Open(string filename)
        {
            return modData.DefaultFileSystem.Open(filename);
        }

        public bool Exists(string filename)
        {
            return modData.DefaultFileSystem.Exists(filename);
        }

    }
}