using System;
using System.Collections.Generic;

namespace EW
{
    /// <summary>
    /// ����ս������
    /// </summary>
    public sealed class World:IDisposable
    {

        public readonly Map Map;

        uint nextAID = 0;

        internal uint NextAID()
        {
            return nextAID++;
        }

        public void Dispose()
        {
        }
    }
}