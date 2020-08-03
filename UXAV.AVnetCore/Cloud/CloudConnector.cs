using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;
using UXAV.AVnetCore.Models;
using UXAV.Logging;

namespace UXAV.AVnetCore.Cloud
{
    internal static class CloudConnector
    {
        private static readonly HttpClient HttpClient;
        private static string _instanceId;
        private static string _applicationName;
        private static string _token;
        private static string _version;
        private static bool _init;
        private static EventWaitHandle _waitHandle;
        private const string BaseUrl = "https://us-central1-avnet-cloud.cloudfunctions.net/deviceApi/v1/apps";

        static CloudConnector()
        {
            HttpClient = new HttpClient();
        }

        private static string CheckinUrl =>
            BaseUrl + $"/{_applicationName}/checkin/{HttpUtility.UrlEncode(InstanceId)}";

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

        internal static void Init(Assembly assembly, string token)
        {
            if (_init) return;
            _init = true;
            _applicationName = assembly.GetName().Name;
            _version = assembly.GetName().Version.ToString();
            _token = token;
            _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironmentOnProgramStatusEventHandler;
            Task.Run(CheckInProcess);
        }

        private static void CheckInProcess()
        {
            while (true)
            {
                Logger.Debug($"{nameof(CloudConnector)} will checkin now...");
                CheckIn();
                if (!_waitHandle.WaitOne(TimeSpan.FromMinutes(5))) continue;
                Logger.Warn($"{nameof(CloudConnector)} leaving checkin process!");
                return;
            }
        }

        private static void CrestronEnvironmentOnProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType != eProgramStatusEventType.Stopping) return;
            _waitHandle.Set();
        }

        private static void CheckIn()
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
                    @model = InitialParametersClass.ControllerPromptName,
                    @serial_number = CrestronEnvironment.SystemInfo.SerialNumber,
                    @app_number = InitialParametersClass.ApplicationNumber,
                    @logger_port = Logger.ListenPort,
                    @version = _version,
                };
                var json = JToken.FromObject(data);
                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                var result = HttpClient.PostAsync(CheckinUrl, content).Result;
                Logger.Log($"{nameof(CloudConnector)}.{nameof(CheckIn)}() result = {result.StatusCode}");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}