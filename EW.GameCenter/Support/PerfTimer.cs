using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
namespace EW.Support
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PerfTimer:IDisposable
    {
        readonly float thresholdMs;
        readonly string name;
        readonly byte depth;
        readonly PerfTimer parent;
        List<PerfTimer> children;
        long ticks;
        /// <summary>
        /// �ṩ���ݵ��̱߳��ش洢
        /// </summary>
        static ThreadLocal<PerfTimer> parentThreadLocal = new ThreadLocal<PerfTimer>();
        public PerfTimer(string name,float thresholdMs = 0)
        {
            this.name = name;
            this.thresholdMs = thresholdMs;

            parent = parentThreadLocal.Value;
            depth = parent == null ? (byte)0 : (byte)(parent.depth + 1);
            parentThreadLocal.Value = this;
            ticks = Stopwatch.GetTimestamp();//��ȡ��ʱ�������еĵ�ǰ�̶���
        }

        public static long LongTickThresholdInStopwatchTicks
        {
            get
            {
                return Stopwatch.Frequency;
            }
        }


        public static void LogLongTick(long startStopwatchTicks,long endStopwatchTicks,string name,object item)
        {

        }

        public void Dispose()
        {

            ticks = Stopwatch.GetTimestamp() - ticks;

            parentThreadLocal.Value = parent;

            if (parent == null)
                Write();
            else if(ElapsedMs > thresholdMs)
            {
                if (parent.children == null)
                    parent.children = new List<PerfTimer>();
                parent.children.Add(this);
            }
                
        }


        float ElapsedMs { get { return 1000f * ticks / Stopwatch.Frequency; } }

        void Write()
        {

        }

    }
}