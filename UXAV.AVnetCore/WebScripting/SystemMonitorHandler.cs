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
            var totalRam = SystemMonitor.TotalRamSize;
            var ramUsed = totalRam - SystemMonitor.RamFree;
            var maxRamUsed = totalRam - SystemMonitor.RamFreeMinimum;
            var data = new
            {
                Cpu = SystemMonitor.CpuUtilization,
                CpuMax = SystemMonitor.MaximumCpuUtilization,
                Memory = (int) Tools.ScaleRange(ramUsed, 0, totalRam, 0, 100),
                MemoryMax = (int) Tools.ScaleRange(maxRamUsed, 0, totalRam, 0, 100),
                Processes = SystemMonitor.NumberOfRunningProcesses,
                ProcessesMax = SystemMonitor.MaximumNumberOfRunningProcesses,
            };
            WriteResponse(data);
        }
    }
}