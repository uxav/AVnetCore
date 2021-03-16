using Crestron.SimplSharpPro.Diagnostics;
using UXAV.AVnetCore.Models;

namespace UXAV.AVnetCore
{
    public static class SystemMonitor
    {
        private static bool _init;

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
            if(_init) return;
            _init = true;
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.CPUStatisticChange += OnSystemMonitorOnCpuStatisticChange;
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.ProcessStatisticChange +=
                OnSystemMonitorOnProcessStatisticChange;
            Crestron.SimplSharpPro.Diagnostics.SystemMonitor.SetUpdateInterval(10);
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
    }
}