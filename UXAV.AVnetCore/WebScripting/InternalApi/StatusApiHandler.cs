using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronAuthentication;
using Newtonsoft.Json.Linq;
using UXAV.AVnetCore.Cloud;
using UXAV.AVnetCore.Models;
using UXAV.Logging;
using s = System;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class StatusApiHandler : ApiRequestHandler
    {
        public StatusApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        // ReSharper disable once UnusedMember.Global
        public void Get()
        {
            try
            {
                if (UxEnvironment.System == null)
                {
                    HandleError(503, "Service Unavailable", "The server is not yet ready to accept requests");
                    return;
                }

                if (Request.RoutePatternArgs.ContainsKey("function"))
                {
                    switch (Request.RoutePatternArgs["function"])
                    {
                        case "boot":
                            WriteResponse(new
                            {
                                @BootStatus = UxEnvironment.System.BootStatus.ToString(),
                                @Message = UxEnvironment.System.BootStatusDescription,
                                @Percentage = UxEnvironment.System.BootProgress,
                            });
                            return;
                        default:
                            HandleNotFound();
                            return;
                    }
                }

                var nl = string.Empty;
                foreach (int c in Environment.NewLine)
                {
                    nl = nl + $"\\x{c:x2}";
                }

                var process = s.Diagnostics.Process.GetCurrentProcess();

                var session = ValidateSession(false);

                WriteResponse(JToken.FromObject(new
                {
                    InitialParametersClass.RoomId,
                    InitialParametersClass.RoomName,
                    SystemBase.SystemName,
                    SystemBase.IpAddress,
                    SystemBase.MacAddress,
                    CrestronEnvironment.SystemInfo.SerialNumber,
                    @CloudInstanceId = CloudConnector.InstanceId,
                    @AppVersion = Server.System.AppVersion.ToString(),
                    @ProgramDirectory = InitialParametersClass.ProgramDirectory.ToString(),
                    @BootStatus = UxEnvironment.System.BootStatus.ToString(),
                    InitialParametersClass.ApplicationNumber,
                    InitialParametersClass.ProgramIDTag,
                    System.BootTime,
                    System.UpTime,
                    @ConsolePort = Logger.ListenPort,
                    @ProcessId = process.Id,
                    process.ProcessName,
                    @Authentication = new
                    {
                        Authentication.Enabled,
                        Authentication.InCloudSync
                    },
                    @Session = session,
                    @TimeZone = new
                    {
                        CrestronEnvironment.GetTimeZone().Name,
                        CrestronEnvironment.GetTimeZone().Formatted,
                        CrestronEnvironment.GetTimeZone().NumericOffset,
                        CrestronEnvironment.GetTimeZone().InDayLightSavings,
                    },
                    @DevicePlatform = CrestronEnvironment.DevicePlatform.ToString(),
                    InitialParametersClass.ControllerPromptName,
                    @Environment = new
                    {
                        @Version = Environment.Version.ToString(),
                        @OSVersion = Environment.OSVersion.VersionString,
                        Environment.MachineName,
                        Environment.ProcessorCount,
                        Environment.Is64BitOperatingSystem,
                        @NewLine = nl,
                    },
                    @TimeInfo = new
                    {
                        RoomClock.Time,
                        RoomClock.Formatted
                    }
                }));
            }
            catch(Exception e)
            {
                HandleError(e);
            }
        }
    }
}