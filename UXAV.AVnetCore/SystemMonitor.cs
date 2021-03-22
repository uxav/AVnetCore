using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UXAV.AVnetCore.Models;
using UXAV.Logging;

namespace UXAV.AVnetCore
{
    public static class SystemMonitor
    {
        private static bool _init;
        private static readonly AutoResetEvent CheckWait = new AutoResetEvent(false);

        private static readonly ConcurrentQueue<MemoryStat>
            MemoryUsageHistory = new ConcurrentQueue<MemoryStat>();

        private static readonly ConcurrentQueue<CpuStat>
            CpuUsageHistory = new ConcurrentQueue<CpuStat>();

        private static bool _programStopping;

        public static ushort CpuUtilization => Crestron.SimplSharpPro.Diagnostics.SystemMonitor.CPUUtilization;

        public static ushort MaximumCpuUtilization =>
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.MaximumCPUUtilization;

        public static uint TotalRamSize => Crestron.SimplSharpPro.Diagnostics.SystemMonitor.TotalRAMSize;
        public static uint RamFree => Crestron.SimplSharpPro.Diagnostics.SystemMonitor.RAMFree;
        public static uint RamFreeMinimum => Crestron.SimplSharpPro.Diagnostics.SystemMonitor.RAMFreeMinimum;

        public static ushort NumberOfRunningProcesses =>
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.NumberOfRunningProcesses;

        public static ushort MaximumNumberOfRunningProcesses =>
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.MaximumNumberOfRunningProcesses;

        internal static void Init()
        {
            if (_init) return;
            _init = true;
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.CPUStatisticChange += OnSystemMonitorOnCpuStatisticChange;
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.ProcessStatisticChange +=
                OnSystemMonitorOnProcessStatisticChange;
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
                    if (CheckWait.WaitOne(TimeSpan.FromSeconds(30)) || _programStopping)
                    {
                        return;
                    }

                    CheckStats();
                }
            });
        }

        private static void CheckStats()
        {
            var memStat = new MemoryStat(Crestron.SimplSharpPro.Diagnostics.SystemMonitor.RAMFree,
                Crestron.SimplSharpPro.Diagnostics.SystemMonitor.RAMFreeMinimum);
            lock (MemoryUsageHistory)
            {
                MemoryUsageHistory.Enqueue(memStat);
                while (MemoryUsageHistory.Count > 120)
                {
                    MemoryUsageHistory.TryDequeue(out var oldMemStat);
                    Logger.Debug($"Removed old memory stat with time: {oldMemStat.Time:u}");
                }
            }

            var cpuStat = new CpuStat(Crestron.SimplSharpPro.Diagnostics.SystemMonitor.CPUUtilization,
                Crestron.SimplSharpPro.Diagnostics.SystemMonitor.MaximumCPUUtilization);
            lock (CpuUsageHistory)
            {
                CpuUsageHistory.Enqueue(cpuStat);
                while (CpuUsageHistory.Count > 120)
                {
                    CpuUsageHistory.TryDequeue(out var oldCpuStat);
                    Logger.Debug($"Removed old cpu stat with time: {oldCpuStat.Time:u}");
                }
            }
        }

        private static void OnSystemMonitorOnProcessStatisticChange(ProcessStatisticChangeEventArgs args)
        {
            if (args.StatisticWhichChanged != eProcessStatisticChange.RAMFreeMinimum) return;
            EventService.Notify(EventMessageType.SystemMonitorMemoryStatsChange,
                new
                {
                    Memory = (int) Tools.ScaleRange(args.TotalRAMSize - args.RAMFree, 0, args.TotalRAMSize, 0, 100),
                    MemoryMax = (int) Tools.ScaleRange(args.TotalRAMSize - args.RAMFreeMinimum, 0, args.TotalRAMSize, 0,
                        100),
                });
        }

        private static void OnSystemMonitorOnCpuStatisticChange(CPUStatisticChangeEventArgs args)
        {
            if (args.StatisticWhichChanged != eCPUStatisticChange.MaximumUtilization) return;
            EventService.Notify(EventMessageType.SystemMonitorCpuStatsChange,
                new
                {
                    Cpu = Crestron.SimplSharpPro.Diagnostics.SystemMonitor.CPUUtilization,
                    CpuMax = Crestron.SimplSharpPro.Diagnostics.SystemMonitor.MaximumCPUUtilization,
                });
        }

        public static MemoryStat[] GetMemoryStats()
        {
            lock (MemoryUsageHistory)
            {
                return MemoryUsageHistory.OrderBy(i => i.Time).ToArray();
            }
        }

        public static CpuStat[] GetCpuStats()
        {
            lock (CpuUsageHistory)
            {
                return CpuUsageHistory.OrderBy(i => i.Time).ToArray();
            }
        }
    }

    public class MemoryStat : SysMonStat
    {
        internal MemoryStat(uint ramFree, uint ramFreeMinimum)
        {
            BytesFree = (int) ramFree;
            BytesFreeMinimum = (int) ramFreeMinimum;
        }

        public int BytesFree { get; }
        public int BytesFreeMinimum { get; }
        public int TotalSize => (int) Crestron.SimplSharpPro.Diagnostics.SystemMonitor.TotalRAMSize;
        public int BytesUsed => TotalSize - BytesFree;
        public int BytesUsedMax => TotalSize - BytesFreeMinimum;
        public override StatType Type => StatType.MemoryUsage;
        public override int PercentageUsed => (int) Tools.ScaleRange(BytesUsed, 0, TotalSize, 0, 100);
        public override int PercentageUsedMax => (int) Tools.ScaleRange(BytesUsedMax, 0, TotalSize, 0, 100);
    }

    public class CpuStat : SysMonStat
    {
        internal CpuStat(ushort cpuUtilization, ushort maximumCpuUtilization)
        {
            PercentageUsed = cpuUtilization;
            PercentageUsedMax = maximumCpuUtilization;
        }

        public override StatType Type => StatType.CpuUsage;
        public override int PercentageUsed { get; }
        public override int PercentageUsedMax { get; }
    }

    public abstract class SysMonStat
    {
        protected SysMonStat()
        {
            Time = DateTime.Now;
        }

        public enum StatType
        {
            MemoryUsage,
            CpuUsage
        }

        public DateTime Time { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public abstract StatType Type { get; }

        public abstract int PercentageUsed { get; }
        public abstract int PercentageUsedMax { get; }
    }
}