using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace UXAV.AVnet.Core.WebScripting
{
    public class SystemMonitorApiHandler : ApiRequestHandler
    {
        public SystemMonitorApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        public async void Get()
        {
            var totalRam = SystemMonitor.TotalRamSize;
            var ramUsed = totalRam - SystemMonitor.RamFree;
            var maxRamUsed = totalRam - SystemMonitor.RamFreeMinimum;
            var history = SystemMonitor.GetStatHistory();
            //var historyTimes = history.Select(sysMonStat => sysMonStat.Time);
            var cpuData = history.Select(sysMonStat => new
            {
                x = sysMonStat.Time,
                y = sysMonStat.CpuUtilization
            });
            var memoryData = history.Select(sysMonStat => new
            {
                x = sysMonStat.Time,
                y = sysMonStat.RamPercent
            });
            var data = new
            {
                Cpu = SystemMonitor.CpuUtilization,
                CpuMax = SystemMonitor.MaximumCpuUtilization,
                Memory = (int)Tools.ScaleRange(ramUsed, 0, totalRam, 0, 100),
                MemoryMax = (int)Tools.ScaleRange(maxRamUsed, 0, totalRam, 0, 100),
                Processes = SystemMonitor.NumberOfRunningProcesses,
                ProcessesMax = SystemMonitor.MaximumNumberOfRunningProcesses,
                HistoryChartData = new
                {
                    CpuData = cpuData,
                    MemoryData = memoryData
                }
            };
            await WriteResponseAsync(data);
        }

        [SecureRequest]
        public async void Post()
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
                        await WriteResponseAsync(true);
                        return;
                }

                await HandleErrorAsync(400, $"Method \"{method}\" not known");
            }
            catch (Exception e)
            {
                await HandleErrorAsync(e);
            }
        }
    }
}