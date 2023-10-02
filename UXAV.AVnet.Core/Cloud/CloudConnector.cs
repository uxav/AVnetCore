using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
        internal static readonly HttpClient HttpClient;
        private static string _instanceId;
        private static string _version;
        private static string _productVersion;
        private static bool _init;
        private static EventWaitHandle _waitHandle;
        private static Uri _checkinUri;
        private static bool _suppressWarning;
        private static Uri _configUploadUri;
        private static bool _uploadConfig = true;
        private static bool _programStopping;

        static CloudConnector()
        {
            HttpClient = new HttpClient();
        }

        internal static string Host { get; private set; }

        private static Uri CheckinUri
        {
            get
            {
                if (_checkinUri == null)
                    _checkinUri = new Uri(
                        $"https://{Host}/api/checkin/v2" +
                        $"/{ApplicationName}/{HttpUtility.UrlEncode(InstanceId)}?token={Token}");

                return _checkinUri;
            }
        }

        private static Uri ConfigUploadUri
        {
            get
            {
                /*return new Uri(
                    "http://172.16.100.200:5001/avnet-cloud/us-central1/appInstanceConfigs/api/configs/v1/submit" +
                    $"/{_applicationName}/{HttpUtility.UrlEncode(InstanceId)}?token={Token}");*/
                if (_configUploadUri == null)
                    _configUploadUri = new Uri(
                        $"https://{Host}/api/configs/v1/submit" +
                        $"/{ApplicationName}/{HttpUtility.UrlEncode(InstanceId)}?token={Token}");

                return _configUploadUri;
            }
        }

        public static string InstanceId
        {
            get
            {
                if (!string.IsNullOrEmpty(_instanceId)) return _instanceId;
                var macString = CrestronEthernetHelper.GetEthernetParameter(
                    CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_MAC_ADDRESS,
                    CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType
                        .EthernetLANAdapter));
                if (string.IsNullOrEmpty(macString)) throw new OperationCanceledException("Unable to get MAC address");
                _instanceId = Regex.Replace(macString, @"[-:]", @"") + "\\" +
                              InitialParametersClass.ApplicationNumber.ToString("D2");
                return _instanceId;
            }
        }

        public static string ApplicationName { get; private set; }

        public static string Token { get; private set; } = "";

        public static string LogsUploadUrl
        {
            get
            {
                if (string.IsNullOrEmpty(Host) || string.IsNullOrEmpty(Token)) return null;
                return $"https://{Host}/api/uploadlogs/v1" +
                       $"/{ApplicationName}/{HttpUtility.UrlEncode(InstanceId)}?token={Token}";
            }
        }

        public static void MarkConfigForUpload()
        {
            _uploadConfig = true;
            _waitHandle?.Set();
        }

        public static void Init(Assembly assembly, string host, string token)
        {
            if (_init) return;
            _init = true;
            Host = host;
            Token = token;
            ApplicationName = assembly.GetName().Name;
            var types = assembly.GetTypes();
            foreach (var type in types)
                try
                {
                    if (!type.IsClass || type.IsNotPublic) continue;
                    if (type.BaseType == null ||
                        type.BaseType != typeof(CrestronControlSystem))
                        continue;

                    ApplicationName = type.Namespace;
                    break;
                }
                catch (Exception e)
                {
                    Logger.Warn(
                        $"Error looking at {type}, {e.GetType().Name}: {e.Message}");
                }

            _version = assembly.GetName().Version.ToString();
            _productVersion = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
            _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironmentOnProgramStatusEventHandler;
            Task.Run(CheckInProcess);
        }

        private static async void CheckInProcess()
        {
            _waitHandle.WaitOne(TimeSpan.FromSeconds(10));
            if (_programStopping) return;

            while (true)
            {
#if DEBUG
                //Logger.Debug($"{nameof(CloudConnector)} will checkin now...");
#endif
                await CheckInAsync();
                if (_uploadConfig)
                    try
                    {
                        await UploadConfigAsync();
                        _uploadConfig = false;
                    }
                    catch (Exception e)
                    {
                        if (!_suppressWarning) Logger.Error(e);
                    }

                _waitHandle.WaitOne(TimeSpan.FromMinutes(1));
                if (!_programStopping) continue;
                Logger.Warn($"{nameof(CloudConnector)} leaving checkin process!");
                return;
            }
        }

        private static void CrestronEnvironmentOnProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType != eProgramStatusEventType.Stopping) return;
            _programStopping = true;
            _waitHandle.Set();
        }

        private static async Task CheckInAsync()
        {
            object sysmon = null;
            if (SystemMonitor.Available)
                sysmon = new
                {
                    SystemMonitor.CpuUtilization,
                    SystemMonitor.MaximumCpuUtilization,
                    SystemMonitor.RamFree,
                    SystemMonitor.RamFreeMinimum,
                    SystemMonitor.TotalRamSize
                };

            try
            {
                var data = new
                {
                    local_ip = SystemBase.IpAddress,
                    host_name = SystemBase.HostName,
                    domain_name = SystemBase.DomainName,
                    dhcp = SystemBase.DhcpStatus,
                    mac_address = SystemBase.MacAddress,
                    up_time = SystemBase.UpTime,
                    system_monitor = sysmon,
                    firmware_version = CrestronEnvironment.OSVersion.Firmware,
                    model = InitialParametersClass.ControllerPromptName,
                    serial_number = CrestronEnvironment.SystemInfo.SerialNumber,
                    app_number = InitialParametersClass.ApplicationNumber,
                    logger_port = Logger.ListenPort,
                    version = _version,
                    productVersion = _productVersion,
                    updateAvailable = UpdateHelper.UpdatesAvailable,
                    device_type = CrestronEnvironment.DevicePlatform.ToString(),
                    room_id = InitialParametersClass.RoomId,
                    room_name = InitialParametersClass.RoomName,
                    system_name = SystemBase.SystemName,
                    program_id_tag = InitialParametersClass.ProgramIDTag,
                    program_directory = InitialParametersClass.ProgramDirectory.ToString(),
                    diagnostics = UxEnvironment.System.GenerateDiagnosticMessagesInternal(),
                    app_rooms = UxEnvironment.GetRooms().Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        screen_name = r.ScreenName,
                        description = r.Description
                    }),
                    ConfigManager.ConfigPath,
                    ConfigRevisionTime = ConfigManager.LastRevisionTime,
                    location = new
                    {
                        local_time = DateTime.Now.ToLocalTime().ToString("s"),
                        time_zone = new
                        {
                            name = CrestronEnvironment.GetTimeZone().Name,
                            offset = CrestronEnvironment.GetTimeZone().NumericOffset,
                            formatted = CrestronEnvironment.GetTimeZone().Formatted,
                            dst = CrestronEnvironment.GetTimeZone().InDayLightSavings
                        },
                        longitude = CrestronEnvironment.Longitude,
                        latitude = CrestronEnvironment.Latitude
                    }
                };
                var json = JToken.FromObject(data);
                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                try
                {
#if DEBUG
                    //Logger.Debug($"Cloud checkin URL is {CheckinUri}");
#endif
                    var result = await HttpClient.PostAsync(CheckinUri, content);
#if DEBUG
                    //Logger.Debug($"{nameof(CloudConnector)}.{nameof(CheckInAsync)}() result = {result.StatusCode}");
#endif
                    result.EnsureSuccessStatusCode();
                    var contents = await result.Content.ReadAsStringAsync();
#if DEBUG
                    //Logger.Debug($"Cloud Rx:\r\n{contents}");
#endif
                    var responseData = JToken.Parse(contents);
                    if (responseData["actions"] != null)
                        foreach (var action in responseData["actions"])
                            try
                            {
                                Logger.Warn($"Received cloud action: {action}");
                                var methodName = (action["method"] ?? "").Value<string>();
                                if (action["args"] != null)
                                {
                                    var args = action["args"].Value<string[]>();
                                    UxEnvironment.System.RunCloudActionInternal(methodName, args);
                                }
                                else
                                {
                                    UxEnvironment.System.RunCloudActionInternal(methodName);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                            }

                    result.Dispose();
                    _suppressWarning = false;
                }
                catch (Exception e)
                {
                    if (_suppressWarning) return;
                    Logger.Warn($"Could not checkin at {CheckinUri}, {e.Message}");
                    _suppressWarning = true;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private static async Task UploadConfigAsync()
        {
            var data = new
            {
                configPath = ConfigManager.ConfigPath,
                revisionDate = ConfigManager.LastRevisionTime.ToUniversalTime(),
                config = ConfigManager.JConfig
            };
            var json = JToken.FromObject(data);
            var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
#if DEBUG
            //Logger.Debug($"Cloud config upload URL is {ConfigUploadUri}");
#endif
            var result = await HttpClient.PostAsync(ConfigUploadUri, content);
#if DEBUG
            //Logger.Debug($"{nameof(CloudConnector)}.{nameof(UploadConfigAsync)}() result = {result.StatusCode}");
#endif
            result.EnsureSuccessStatusCode();
            var contents = await result.Content.ReadAsStringAsync();
#if DEBUG
            //Logger.Debug($"Cloud Rx:\r\n{contents}");
#endif
            result.Dispose();
        }

        public static async void PublishLogsAsync()
        {
            try
            {
                Logger.Highlight("Publishing logs to cloud...");
                var zipStream = await DiagnosticsArchiveTool.CreateArchiveAsync();
                using (var content = new MultipartFormDataContent())
                {
                    zipStream.Position = 0;
                    var fileContent = new StreamContent(zipStream);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MimeMapping.GetMimeMapping(".zip"));
                    content.Add(fileContent, "logs",
                        $"app_report_{InitialParametersClass.RoomId}_{DateTime.Now:yyyyMMddTHHmmss}.zip");
                    Logger.Debug($"Content Headers:\r\n{fileContent.Headers}");
                    Logger.Debug($"Request Headers:\r\n{content.Headers}");
                    var result = HttpClient.PostAsync(LogsUploadUrl, content).Result;
                    result.EnsureSuccessStatusCode();
                    Logger.Highlight($"Logs submitted. Result = {result.StatusCode}");
                    var response = await result.Content.ReadAsStringAsync();
                    Logger.Debug($"Response: {response}");
                    result.Dispose();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}