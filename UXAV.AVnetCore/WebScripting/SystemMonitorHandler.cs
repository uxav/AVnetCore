using Crestron.SimplSharpPro.Diagnostics;

namespace UXAV.AVnetCore.WebScripting
{
    public class SystemMonitorHandler : ApiRequestHandler
    {
        public SystemMonitorHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        public void Get()
        {
            var totalRam = SystemMonitor.TotalRAMSize;
            var ramUsed = totalRam - SystemMonitor.RAMFree;
            var maxRamUsed = totalRam - SystemMonitor.RAMFreeMinimum;
            var data = new
            {
                Cpu = SystemMonitor.CPUUtilization,
                CpuMax = SystemMonitor.MaximumCPUUtilization,
                Memory = (int) Tools.ScaleRange(ramUsed, 0, totalRam, 0, 100),
                MemoryMax = (int) Tools.ScaleRange(maxRamUsed, 0, totalRam, 0, 100),
                Processes = SystemMonitor.NumberOfRunningProcesses,
                ProcessesMax = SystemMonitor.MaximumNumberOfRunningProcesses,
            };
            WriteResponse(data);
        }
    }
}