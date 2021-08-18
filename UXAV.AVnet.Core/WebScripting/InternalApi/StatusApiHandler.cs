using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronAuthentication;
using Crestron.SimplSharpPro;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Cloud;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;
using s = System;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
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

                var userPageAuth = false;
                if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance)
                {
                    userPageAuth = Authentication.UserPageAuthEnabled;
                }

                WriteResponse(JToken.FromObject(new
                {
                    InitialParametersClass.RoomId,
                    InitialParametersClass.RoomName,
                    SystemBase.SystemName,
                    SystemBase.IpAddress,
                    SystemBase.MacAddress,
                    CrestronEnvironment.SystemInfo.SerialNumber,
                    @Firmware = InitialParametersClass.FirmwareVersion,
                    @AVNetName = UxEnvironment.Name,
                    @AVNetVersion = UxEnvironment.Version.ToString(),
                    @AVNetAssemblyVersion = UxEnvironment.AssemblyVersion.ToString(),
                    @CloudInstanceId = CloudConnector.InstanceId,
                    @AppVersion = Server.System.AppVersion.ToString(),
                    @AppAssemblyVersion = Server.System.AppAssemblyVersion.ToString(),
                    @AppBuildTime = System.ProgramBuildTime,
                    @Include4Dat = System.Include4DatInfo,
                    @ProgramDirectory = InitialParametersClass.ProgramDirectory.ToString(),
                    @BootStatus = UxEnvironment.System.BootStatus.ToString(),
                    InitialParametersClass.ApplicationNumber,
                    InitialParametersClass.ProgramIDTag,
                    SystemBase.BootTime,
                    SystemBase.UpTime,
                    @ConsolePort = Logger.ListenPort,
                    @ProcessId = process.Id,
                    @CrestronSecureStorage = CrestronSecureStorage.Supported,
                    process.ProcessName,
                    @Authentication = new
                    {
                        Authentication.Enabled,
                        Authentication.InCloudSync,
                        Authentication.AdministratorExist,
                        @UserPageAuthEnabled = userPageAuth,
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
                    @Location = new
                    {
                        CrestronEnvironment.Latitude,
                        CrestronEnvironment.Longitude,
                    },
                    @TimeInfo = new
                    {
                        RoomClock.Time,
                        RoomClock.Formatted
                    },
                    @BACnet = new
                    {
                        Supported = System.ControlSystem.SupportsBACNet,
                        Registered = System.ControlSystem.ControllerBACnetDevice.Registered,
                        System.ControlSystem.ControllerBACnetDevice.EndpointsLimit,
                        System.ControlSystem.ControllerBACnetDevice.BACnetDiscoveryEnabled,
                        BACnet.IsLicensed
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