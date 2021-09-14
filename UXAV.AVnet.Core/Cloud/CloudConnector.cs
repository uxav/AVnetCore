using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Config;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Cloud
{
    internal static class CloudConnector
    {
        private static readonly HttpClient HttpClient;
        private static string _instanceId;
        private static string _applicationName;
        private static string _version;
        private static bool _init;
        private static EventWaitHandle _waitHandle;
        private static Uri _checkinUri;
        private static bool _suppressWarning;

        static CloudConnector()
        {
            HttpClient = new HttpClient();
        }

        private static Uri CheckinUri
        {
            get
            {
                if (_checkinUri == null)
                {
                    _checkinUri = new Uri($"https://avnet.io/api/checkin/v1/{_applicationName}/{HttpUtility.UrlEncode(InstanceId)}");
                }

                return _checkinUri;
            }
        }


        internal static string InstanceId
        {
            get
            {
                if (!string.IsNullOrEmpty(_instanceId)) return _instanceId;
                var macString = CrestronEthernetHelper.GetEthernetParameter(
                    CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_MAC_ADDRESS,
                    CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType
                        .EthernetLANAdapter));
                _instanceId = Regex.Replace(macString, @"[-:]", @"") + "\\" +
                              InitialParametersClass.ApplicationNumber.ToString("D2");
                return _instanceId;
            }
        }

        internal static void Init(Assembly assembly)
        {
            if (_init) return;
            _init = true;
            _applicationName = assembly.GetName().Name;
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                try
                {
                    if (!type.IsClass || type.IsNotPublic) continue;
                    if (type.BaseType == null ||
                        type.BaseType != typeof(CrestronControlSystem))
                        continue;

                    _applicationName = type.Namespace;
                    break;
                }
                catch (Exception e)
                {
                    Logger.Warn(
                        $"Error looking at {type}, {e.GetType().Name}: {e.Message}");
                }
            }

            _version = assembly.GetName().Version.ToString();
            _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironmentOnProgramStatusEventHandler;
            Task.Run(CheckInProcess);
        }

        private static async void CheckInProcess()
        {
            while (true)
            {
                Logger.Debug($"{nameof(CloudConnector)} will checkin now...");
                await CheckInAsync();
                if (!_waitHandle.WaitOne(TimeSpan.FromMinutes(1))) continue;
                Logger.Warn($"{nameof(CloudConnector)} leaving checkin process!");
                return;
            }
        }

        private static void CrestronEnvironmentOnProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType != eProgramStatusEventType.Stopping) return;
            _waitHandle.Set();
        }

        private static async Task CheckInAsync()
        {
            try
            {
                var data = new
                {
                    @local_ip = SystemBase.IpAddress,
                    @host_name = SystemBase.HostName,
                    @domain_name = SystemBase.DomainName,
                    @dhcp = SystemBase.DhcpStatus,
                    @mac_address = SystemBase.MacAddress,
                    @up_time = SystemBase.UpTime,
                    @firmware_version = CrestronEnvironment.OSVersion.Firmware,
                    @model = InitialParametersClass.ControllerPromptName,
                    @serial_number = CrestronEnvironment.SystemInfo.SerialNumber,
                    @app_number = InitialParametersClass.ApplicationNumber,
                    @logger_port = Logger.ListenPort,
                    @version = _version,
                    @device_type = CrestronEnvironment.DevicePlatform.ToString(),
                    @room_id = InitialParametersClass.RoomId,
                    @room_name = InitialParametersClass.RoomName,
                    @program_id_tag = InitialParametersClass.ProgramIDTag,
                    @program_directory = InitialParametersClass.ProgramDirectory.ToString(),
                    @diagnostics = UxEnvironment.System.GenerateDiagnosticMessagesInternal(),
                    @app_rooms = UxEnvironment.GetRooms().Select(r => new
                    {
                        @id = r.Id,
                        @name = r.Name,
                        @screen_name = r.ScreenName,
                        @description = r.Description,
                    }),
                    ConfigManager.ConfigPath,
                    @ConfigRevisionTime = ConfigManager.LastRevisionTime,
                    @location = new
                    {
                        @local_time = DateTime.Now.ToLocalTime().ToString("s"),
                        @time_zone = new
                        {
                            @name = CrestronEnvironment.GetTimeZone().Name,
                            @offset = CrestronEnvironment.GetTimeZone().NumericOffset,
                            @formatted = CrestronEnvironment.GetTimeZone().Formatted,
                            @dst = CrestronEnvironment.GetTimeZone().InDayLightSavings,
                        },
                        @longitude = CrestronEnvironment.Longitude,
                        @latitude = CrestronEnvironment.Latitude
                    }
                };
                var json = JToken.FromObject(data);
                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                try
                {
                    Logger.Debug($"Cloud checkin URL is {CheckinUri}");
                    var result = await HttpClient.PostAsync(CheckinUri, content);
                    Logger.Debug($"{nameof(CloudConnector)}.{nameof(CheckInAsync)}() result = {result.StatusCode}");
#if DEBUG
                    var contents = await result.Content.ReadAsStringAsync();
                    Logger.Debug($"Cloud Rx:\r\n{contents}");
#endif
                    result.Dispose();
                    _suppressWarning = false;
                }
                catch (Exception e)
                {
                    if(_suppressWarning) return;
                    Logger.Warn($"Could not checkin to cloud, {e.Message}");
                    _suppressWarning = true;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}