using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.Diagnostics;
using UXAV.AVnet.Core.Models;

namespace UXAV.AVnet.Core
{
    public static class SystemMonitor
    {
        private static bool _init;
        private static readonly AutoResetEvent CheckWait = new AutoResetEvent(false);

        private static readonly ConcurrentQueue<SysMonStat>
            StatHistory = new ConcurrentQueue<SysMonStat>();

        private static bool _programStopping;

        public static ushort CpuUtilization => Crestron.SimplSharpPro.Diagnostics.SystemMonitor.CPUUtilization;

        public static ushort MaximumCpuUtilization =>
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.MaximumCPUUtilization;

        public static long TotalRamSize => Crestron.SimplSharpPro.Diagnostics.SystemMonitor.TotalRAMSize * 1000;
        public static long RamFree => Crestron.SimplSharpPro.Diagnostics.SystemMonitor.RAMFree * 1000;
        public static long RamFreeMinimum => Crestron.SimplSharpPro.Diagnostics.SystemMonitor.RAMFreeMinimum * 1000;

        public static ushort NumberOfRunningProcesses =>
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.NumberOfRunningProcesses;

        public static ushort MaximumNumberOfRunningProcesses =>
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.MaximumNumberOfRunningProcesses;

        public static bool Available => CrestronEnvironment.DevicePlatform != eDevicePlatform.Server && _init;

        internal static void Init()
        {
            if (_init) return;
            _init = true;
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.CPUStatisticChange += OnSystemMonitorOnCpuStatisticChange;
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.ProcessStatisticChange += SystemMonitorOnProcessStatisticChange;
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.SetUpdateInterval(10);
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type != eProgramStatusEventType.Stopping) return;
                _programStopping = true;
                CheckWait.Set();
            };
            Task.Run(() =>
            {
                while (true)
                {
                    if (CheckWait.WaitOne(TimeSpan.FromSeconds(10)) || _programStopping)
                    {
                        return;
                    }

                    CheckStats();
                }
            });
        }

        private static void CheckStats()
        {
            var stat = new SysMonStat(RamFree, CpuUtilization, NumberOfRunningProcesses);
            lock (StatHistory)
            {
                StatHistory.Enqueue(stat);
                while (StatHistory.Count > 2000)
                {
                    StatHistory.TryDequeue(out var oldMemStat);
                    //Logger.Debug($"Removed old memory stat with time: {oldMemStat.Time:u}");
                }
            }
            EventService.Notify(EventMessageType.SystemMonitorStatsChange, new
            {
                CpuValue = new
                {
                    x = stat.Time,
                    y = stat.CpuUtilization
                },
                MemoryValue = new
                {
                    x = stat.Time,
                    y = stat.RamPercent
                }
            });
        }

        private static void OnSystemMonitorOnCpuStatisticChange(CPUStatisticChangeEventArgs args)
        {
        }

        private static void SystemMonitorOnProcessStatisticChange(ProcessStatisticChangeEventArgs args)
        {
        }

        public static SysMonStat[] GetStatHistory()
        {
            lock (StatHistory)
            {
                return StatHistory.OrderByDescending(i => i.Time).ToArray();
            }
        }
    }

    public class SysMonStat
    {
        internal SysMonStat(long ramFree, ushort cpuUtilization, uint processes)
        {
            Time = DateTime.Now;
            RamFree = ramFree;
            CpuUtilization = cpuUtilization;
            NumberOfRunningProcesses = processes;
        }

        public DateTime Time { get; }

        public long RamFree { get; }

        public string RamFreeLabel => Tools.PrettyByteSize(RamUsed, 1);

        public long RamUsed => SystemMonitor.TotalRamSize - RamFree;

        public string RamUsedLabel => Tools.PrettyByteSize(RamUsed, 1);

        public double RamPercent => Tools.ScaleRange(RamUsed, 0, SystemMonitor.TotalRamSize, 0, 100, 1);

        public uint CpuUtilization { get; }
        public uint NumberOfRunningProcesses { get; }
    }
}