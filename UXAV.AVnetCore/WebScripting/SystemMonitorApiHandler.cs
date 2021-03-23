using System;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnetCore.WebScripting
{
    public class SystemMonitorApiHandler : ApiRequestHandler
    {
        public SystemMonitorApiHandler(WebScriptingServer server, WebScriptingRequest request)
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
                MemoryHistory = SystemMonitor.GetMemoryStats(),
                CpuHistory = SystemMonitor.GetCpuStats(),
            };
            WriteResponse(data);
        }

        [SecureRequest]
        public void Post()
        {
            try
            {
                var reader = new StreamReader(Request.InputStream);
                var json = JToken.Parse(reader.ReadToEnd());
                var method = (json["method"] ?? throw new InvalidOperationException("No method stated"))
                    .Value<string>();
                switch (method)
                {
                    case "resetmaxvalues":
                        Crestron.SimplSharpPro.Diagnostics.SystemMonitor.ResetMaximums();
                        WriteResponse(true);
                        return;
                }
                HandleError(400, "Bad Request", $"Method \"{method}\" not known");
                return;
            }
            catch (Exception e)
            {
                HandleError(e);
            }
        }
    }
}