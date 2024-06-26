using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronDataStore;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.Fusion;
using UXAV.AVnet.Core.Cloud;
using UXAV.AVnet.Core.Config;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.AVnet.Core.Models.Diagnostics;
using UXAV.AVnet.Core.UI;
using UXAV.AVnet.Core.UI.Ch5;
using UXAV.AVnet.Core.WebScripting;
using UXAV.AVnet.Core.WebScripting.Download;
using UXAV.AVnet.Core.WebScripting.InternalApi;
using UXAV.AVnet.Core.WebScripting.StaticFiles;
using UXAV.Logging;
using Directory = Crestron.SimplSharp.CrestronIO.Directory;

namespace UXAV.AVnet.Core.Models
{
    public abstract class SystemBase
    {
        public enum EBootStatus
        {
            Booting,
            LoadingConfig,
            Initializing,
            Running,
            DidNotBoot,
            Rebooting
        }

        public enum EUnits
        {
            Imperial,
            Metric
        }

        private static string _programRootDirectory;
        private static string _systemName;
        private static string _runtimeGuid;
        private static bool _appIsUpdated;
        private static string _serialNumber;
        private static string _macAddress;
        private static string _ipAddress;
        private static string _dhcpStatus;
        private static string _domainName;
        private static string _hostName;
        private static string _appVersion;
        private readonly string _initialConfig;
        private readonly List<IInitializable> _itemsToInitialize = new List<IInitializable>();
        internal readonly Dictionary<uint, IDevice> DevicesDict = new Dictionary<uint, IDevice>();

        protected SystemBase(CrestronControlSystem controlSystem)
        {
            CrestronEnvironment.ProgramStatusEventHandler += SystemStoppingInternal;
            Logger.MessageLogged += message => { EventService.Notify(EventMessageType.LogEntry, message); };

            Logger.Highlight("{0}.ctor()", GetType().FullName);

            Logger.Highlight("System.ctor()");

            UxEnvironment.System = this;
            UxEnvironment.ControlSystem = controlSystem;
            UxEnvironment.InitConsoleCommands();
            CipDevices.Init(controlSystem);

            _initialConfig = ConfigManager.JConfig?.ToString();

            DiagnosticService.RegisterSystemCallback(GenerateDiagnosticMessagesInternal);

            Logger.AddCommand((argString, args, connection, respond) => GC.Collect(), "GarbageCollect",
                "Run the garbage collector");

            RoomClock.Start();
            Scheduler.Init();
            UpdateBootStatus(EBootStatus.Booting, "System is booting", 0);

            try
            {
                var infoFile = ProgramApplicationDirectory + "/ProgramInfo.config";
                using (var file = File.OpenRead(infoFile))
                {
                    var reader = new XmlTextReader(file);
                    var elementName = string.Empty;
                    while (reader.Read())
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                elementName = reader.Name;
                                break;
                            case XmlNodeType.EndElement:
                                elementName = null;
                                break;
                            case XmlNodeType.Text:
                                switch (elementName)
                                {
                                    case "TargetFramework":
                                        TargetFramework = reader.Value;
                                        break;
                                    case "Include4.dat":
                                        Include4DatInfo = reader.Value;
                                        break;
                                    case "CompiledOn":
                                        ProgramBuildTime = DateTime.Parse(reader.Value).ToUniversalTime();
                                        break;
                                }

                                break;
                        }

                    reader.Close();
                }
            }
            catch (Exception e)
            {
                Logger.Warn($"Could not load info from ProgramInfo.config, {e.Message}");
            }

            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance) SystemMonitor.Init();

            try
            {
                Logger.Highlight("Calling CrestronDataStoreStatic.InitCrestronDataStore()");
                var response = CrestronDataStoreStatic.InitCrestronDataStore();
                if (response == CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
                    Logger.Success($"InitCrestronDataStore() = {response}");
                else
                    Logger.Error($"CrestronDataStore Init Error: {response}");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            ControlSystem = controlSystem;

            AppAssembly = Assembly.GetCallingAssembly();

            Logger.Log("FrameworkDescription: {0}", RuntimeInformation.FrameworkDescription);
            Logger.Log("ProcessArchitecture: {0}", RuntimeInformation.ProcessArchitecture);
            Logger.Log("OSArchitecture: {0}", RuntimeInformation.OSArchitecture);
            Logger.Log("OSDescription: {0}", RuntimeInformation.OSDescription);
            Logger.Log("Include4.dat Version: {0}", Include4DatInfo);
            Logger.Log("Local Time is {0}", DateTime.Now);
            var tz = CrestronEnvironment.GetTimeZone();
            Logger.Log("ProgramIDTag: {0}", InitialParametersClass.ProgramIDTag);
            Logger.Log("ApplicationNumber: {0}", InitialParametersClass.ApplicationNumber);
            Logger.Log("FirmwareVersion: {0}", InitialParametersClass.FirmwareVersion);
            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance)
                Logger.Log("SerialNumber: {0}", CrestronEnvironment.SystemInfo.SerialNumber);
            Logger.Log("App Info: {0}", AppAssembly.GetName().FullName);
            Logger.Log("App Version: {0}", AppVersion);
            var versionInfo = FileVersionInfo.GetVersionInfo(AppAssembly.Location);
            Logger.Log("App File Version: {0}", versionInfo.FileVersion);
            Logger.Log("App Product Version: {0}", versionInfo.ProductVersion);
            Logger.Log("App Assembly Version: {0}", AppAssembly.GetName().Version);
            Logger.Log("{0} Version: {1}", UxEnvironment.Name, UxEnvironment.Version);
            Logger.Log("{0} Product Version: {1}", UxEnvironment.Name, UxEnvironment.ProductVersion);
            Logger.Log("{0} Assembly Version: {1}", UxEnvironment.Name, UxEnvironment.AssemblyVersion);
#if DEBUG
            Logger.Log("AVnetCore running is DEBUG build! 🕷️");
#endif
            Logger.Log($"Target framework: {TargetFramework}");
            Logger.Log($"Program Info states build time as: {ProgramBuildTime:R}");
            Logger.Debug("ProcessId: {0}", Process.GetCurrentProcess().Id);
            Logger.Log("Room Name: {0}", InitialParametersClass.RoomName);
            Logger.Log("TimeZone: 🌍 {0}{1}", tz.Formatted, tz.InDayLightSavings ? " (DST)" : string.Empty);
            Logger.Debug("ProgramRootDirectory = {0}", ProgramRootDirectory);
            Logger.Debug("ProgramApplicationDirectory = {0}", ProgramApplicationDirectory);
            Logger.Debug("ProgramUserDirectory = {0}", ProgramUserDirectory);
            Logger.Debug("ProgramNvramDirectory = {0}", ProgramNvramDirectory);
            Logger.Debug("ProgramHtmlDirectory = {0}", ProgramHtmlDirectory);
            Logger.Debug("DevicePlatform = {0}", CrestronEnvironment.DevicePlatform);
            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)
                try
                {
                    dynamic deviceInfo = Vc4WebApi.GetDeviceInfoAsync().Result;
                    dynamic programInstance = Vc4WebApi.GetProgramInstanceAsync().Result;
                    dynamic programLibrary = Vc4WebApi.GetProgramLibraryAsync().Result;
                    var programInfo = programLibrary[programInstance.ProgramLibraryId.ToString()];
                    Logger.Log("Server Name: {0}", deviceInfo.Name);
                    Logger.Log("Server Serial Number: {0}", deviceInfo.DeviceId);
                    Logger.Log("Server Version: {0}", deviceInfo.ApplicationVersion);
                    Logger.Log("Server Build Date: {0}", deviceInfo.BuildDate);
                    Logger.Log("Server ProgramInstanceId = {0}", programInstance.ProgramInstanceId);
                    Logger.Log("Server ProgramLibraryId = {0}", programInstance.ProgramLibraryId);
                    Logger.Log("Server Program FriendlyName = {0}", programInfo.FriendlyName);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error logging VC-4 info from API, {e.Message}");
                }

            Logger.Log("ControlSystem.NumberOfEthernetAdapters = {0}", ControlSystem.NumberOfEthernetAdapters);
            Logger.Log("ControlSystem.NumberOfComPorts = {0}", ControlSystem.NumberOfComPorts);
            Logger.Log("ControlSystem.NumberOfVersiPorts = {0}", ControlSystem.NumberOfVersiPorts);
            Logger.Log("ControlSystem.NumberOfDigitalInputPorts = {0}", ControlSystem.NumberOfDigitalInputPorts);
            Logger.Log("ControlSystem.NumberOfRelayPorts = {0}", ControlSystem.NumberOfRelayPorts);
            Logger.Log("ControlSystem.NumberOfIROutputPorts = {0}", ControlSystem.NumberOfIROutputPorts);
            Logger.Log("ControlSystem.SupportsBluetooth = {0}", ControlSystem.SupportsBluetooth);
            Logger.Log("ControlSystem.SupportsBACNet = {0}", ControlSystem.SupportsBACNet);
            Logger.Log("ControlSystem.SupportsInternalRFGateway = {0}", ControlSystem.SupportsInternalRFGateway);
            Logger.Log("ControlSystem.SupportsInternalAirMedia = {0}", ControlSystem.SupportsInternalAirMedia);
            Logger.Log("ControlSystem.SupportsSystemMonitor = {0}", ControlSystem.SupportsSystemMonitor);
            Logger.Log("ControlSystem.SupportsCresnet = {0}", ControlSystem.SupportsCresnet);

            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (var resource in resources) Logger.Log("Resource found: {0}", resource);

            UpdateBootStatus(EBootStatus.Booting, "Checking upgrade requirements", 0);
            _appIsUpdated = CheckIfNewVersion(AppAssembly);
            Logger.Warn("App is new version: {0}", _appIsUpdated);
            if (_appIsUpdated)
            {
                Logger.Log("App is new version, running upgrade scripts...");
                try
                {
                    UpdateBootStatus(EBootStatus.Booting, "Running upgrade scripts", 0);
                    AppShouldRunUpgradeScriptsInternal();
                    Thread.Sleep(500);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
            else
            {
                Logger.Log("App is not upgrading. Remains at {0}", AppAssembly.GetName().Version);
            }

            UpdateBootStatus(EBootStatus.Booting, "Starting web scripting services", 0);
            Logger.Highlight("Loading static file web scripting server at \"/files\"");
            try
            {
                FileServer = new WebScriptingServer(this, "files");
                FileServer.AddRoute(@"/files/static/<filepath:[\/\w\.\-\[\]\(\)\x20]+>", typeof(InternalFileHandler));
                FileServer.AddRoute(@"/files/user/<filepath:[\/\w\.\-\[\]\(\)\x20]+>", typeof(UserFileRequestHandler));
                FileServer.AddRoute(@"/files/nvram/<filepath:[\/\w\.\-\[\]\(\)\x20]+>",
                    typeof(NvramFileRequestHandler));
                FileServer.AddRoute(@"/files/xpanels/<filename:Core3XPanel_\w{2}\.(?:vtz|c3p)$>",
                    typeof(XPanelResourceFileHandler));
                FileServer.AddRoute(@"/files/service", typeof(ServicePackageFileHandler));
            }
            catch (Exception e)
            {
                Logger.Warn("Could not load static file web scripting server, {0}", e.Message);
            }

            Logger.Highlight("Loading API web scripting server at \"/api\"");
            try
            {
                ApiServer = new ApiWebScriptingServer(this, "api");
                ApiServer.AddRoute(@"/api/status/<function:\w+>", typeof(StatusApiHandler));
                ApiServer.AddRoute(@"/api/status", typeof(StatusApiHandler));
                ApiServer.AddRoute(@"/api/virtualcontrol/<method:\w+>", typeof(Vc4StatusApiHandler));
                ApiServer.AddRoute(@"/api/config/<function:plist>/<key:\w+>", typeof(ConfigApiHandler));
                ApiServer.AddRoute(@"/api/config/<function:\w+>", typeof(ConfigApiHandler));
                ApiServer.AddRoute(@"/api/config", typeof(ConfigApiHandler));
                ApiServer.AddRoute(@"/api/rooms", typeof(RoomsApiHandler));
                ApiServer.AddRoute(@"/api/rooms/<id:\d+>", typeof(RoomsApiHandler));
                ApiServer.AddRoute(@"/api/rooms/<id:\d+>/<method:\w+>", typeof(RoomsApiHandler));
                ApiServer.AddRoute(@"/api/sources", typeof(SourcesApiHandler));
                ApiServer.AddRoute(@"/api/events/<method:\w+>", typeof(EventsApiHandler));
                ApiServer.AddRoute(@"/api/events/<method:\w+>/<id:\d+>", typeof(EventsApiHandler));
                ApiServer.AddRoute(@"/api/logs", typeof(LoggerApiHandler));
                ApiServer.AddRoute(@"/api/plog", typeof(PlogApiHandler));
                ApiServer.AddRoute(@"/api/authentication", typeof(AuthenticationApiHandler));
                ApiServer.AddRoute(@"/api/passwords", typeof(PasswordsApiHandler));
                ApiServer.AddRoute(@"/api/appcontrol", typeof(AppControlApiHandler));
                ApiServer.AddRoute(@"/api/autodiscovery", typeof(AutoDiscoveryApiHandler));
                ApiServer.AddRoute(@"/api/console", typeof(ConsoleApiHandler));
                ApiServer.AddRoute(@"/api/diagnostics", typeof(DiagnosticsApiHandler));
                ApiServer.AddRoute(@"/api/sysmon", typeof(SystemMonitorApiHandler));
                ApiServer.AddRoute(@"/api/upload/<fileType:\w+>", typeof(FileUploadApiHandler));
                ApiServer.AddRoute(@"/api/upload/uploadedfiles/<fileType:\w+>", typeof(UploadedFilesApiHandler));
                ApiServer.AddRoute(@"/api/xpanels", typeof(XPanelDetailsApiHandler));
                ApiServer.AddRoute(@"/api/ch5/<page:\w+>", typeof(Ch5StatusApiHandler));
                ApiServer.AddRoute(@"/api/swupdate", typeof(UpdatesApiHandler));
            }
            catch (Exception e)
            {
                Logger.Warn("Could not load API web scripting server, {0}", e.Message);
            }

            // Wait for above handlers to start accepting requests and update.
            Thread.Sleep(1000);
            Logger.Success(".ctor() Complete", true);
            UpdateBootStatus(EBootStatus.Booting, "Waiting to initialize...", 0);
        }

        public CrestronControlSystem ControlSystem { get; }

        protected WebScriptingServer FileServer { get; }

        protected WebScriptingServer ApiServer { get; }

        protected WebScriptingServer WebAppServer { get; private set; }

        internal static Assembly AppAssembly { get; private set; }

        /// <summary>
        ///  The name of the system
        /// </summary>
        public static string SystemName
        {
            get => string.IsNullOrEmpty(_systemName) ? InitialParametersClass.RoomName : _systemName;
            set => _systemName = value;
        }

        /// <summary>
        ///  The time the app was started
        /// </summary>
        public static DateTime BootTime { get; } = DateTime.Now;

        /// <summary>
        ///  The uptime of the system
        /// </summary>
        public static TimeSpan UpTime => DateTime.Now - BootTime;

        private static short AdapterIdForLan =>
            CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType.EthernetLANAdapter);

        /// <summary>
        ///   The hostname of the main NIC (LAN A) on the processor or server
        /// </summary>
        public static string HostName
        {
            get
            {
                if (string.IsNullOrEmpty(_hostName))
                    _hostName = CrestronEthernetHelper.GetEthernetParameter(
                        CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_HOSTNAME, AdapterIdForLan);
                return _hostName;
            }
        }

        /// <summary>
        ///  The domain name of the main NIC (LAN A) on the processor or server
        /// </summary>
        public static string DomainName
        {
            get
            {
                if (string.IsNullOrEmpty(_domainName))
                    _domainName = CrestronEthernetHelper.GetEthernetParameter(
                        CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_DOMAIN_NAME, AdapterIdForLan);
                return _domainName;
            }
        }

        /// <summary>
        ///   The DHCP status of the main NIC (LAN A) on the processor or server
        /// </summary>
        public static string DhcpStatus
        {
            get
            {
                if (string.IsNullOrEmpty(_dhcpStatus))
                    _dhcpStatus = CrestronEthernetHelper.GetEthernetParameter(
                        CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_STARTUP_DHCP_STATUS, AdapterIdForLan);
                return _dhcpStatus;
            }
        }

        /// <summary>
        ///   The IP address of the main NIC (LAN A) on the processor or server
        /// </summary>
        public static string IpAddress
        {
            get
            {
                if (!string.IsNullOrEmpty(_ipAddress) && _ipAddress != "0.0.0.0") return _ipAddress;
                var value = CrestronEthernetHelper.GetEthernetParameter(
                    CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, AdapterIdForLan);
                if (value.ToUpper() == "INVALID VALUE")
                {
                    Logger.Error("IP Address is invalid, returning");
                    return value;
                }

                _ipAddress = value;

                return _ipAddress;
            }
        }

        /// <summary>
        ///   The MAC address of the main NIC (LAN A) on the processor or server
        /// </summary>
        public static string MacAddress
        {
            get
            {
                if (string.IsNullOrEmpty(_macAddress))
                    _macAddress = CrestronEthernetHelper.GetEthernetParameter(
                        CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_MAC_ADDRESS, AdapterIdForLan);
                return _macAddress.ToLower();
            }
        }

        /// <summary>
        ///    The serial number of the processor or server
        /// </summary>
        public static string SerialNumber
        {
            get
            {
                if (string.IsNullOrEmpty(_serialNumber))
                    _serialNumber = CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance
                        ? CrestronEnvironment.SystemInfo.SerialNumber
                        : (string)((dynamic)Vc4WebApi.GetDeviceInfoAsync().Result).DeviceId;

                return _serialNumber;
            }
        }

        public virtual string AppName => AppAssembly.GetName().Name;

        /// <summary>
        ///  The version of the app
        /// </summary>
        public virtual string AppVersion
        {
            get
            {
                if (_appVersion != null) return _appVersion;
                var vi = FileVersionInfo.GetVersionInfo(AppAssembly.Location);
                _appVersion = $"{vi.ProductMajorPart}.{vi.ProductMinorPart}.{vi.ProductBuildPart}";

                return _appVersion;
            }
        }

        /// <summary>
        ///   The unique identifier for the runtime of the app
        ///   <para>Will be re-generated if the application restarts</para>
        /// </summary>
        public static string RuntimeGuid
        {
            get
            {
                if (!string.IsNullOrEmpty(_runtimeGuid)) return _runtimeGuid;
                if (_appIsUpdated)
                {
                    _runtimeGuid = Guid.NewGuid().ToString();
                    ConfigManager.SetPropertyListItemWithKey("RuntimeGuid", _runtimeGuid);
                    return _runtimeGuid;
                }

                _runtimeGuid = ConfigManager.GetOrCreatePropertyListItem("RuntimeGuid", Guid.NewGuid().ToString());
                return _runtimeGuid;
            }
        }

        public virtual Version AppAssemblyVersion => AppAssembly.GetName().Version;

        /// <summary>
        ///    The directory of the running program
        /// </summary>
        /// <returns>
        ///     eDevicePlatform.Server if running on a VC-4 or similar server setup
        ///     eDevicePlatform.Appliance if running on a Crestron hardware processor such as a CP4 or MC4
        /// </returns>
        public static eDevicePlatform DevicePlatform => CrestronEnvironment.DevicePlatform;

        /// <summary>
        ///     Returns the application's 'root' directory. This is where NVRAM and HTML folders can be found. No trailing
        ///     directory separator at the end.
        /// </summary>
        public static string ProgramRootDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_programRootDirectory))
                    _programRootDirectory = Directory.GetApplicationRootDirectory();

                return _programRootDirectory;
            }
        }

        /// <summary>
        ///     The directory of the running program
        /// </summary>
        public static string ProgramApplicationDirectory => InitialParametersClass.ProgramDirectory.ToString();

        public static string TempFileDirectory
        {
            get
            {
                var path = ProgramNvramDirectory + "/tmp";
                if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);

                return path;
            }
        }

        /// <summary>
        ///   The user directory for the processor or program instance on a server
        /// </summary>
        public static string ProgramUserDirectory => ProgramRootDirectory + "/user";

        /// <summary>
        ///    The NVRAM directory for the processor or program instance on a server
        /// </summary>
        public static string ProgramNvramDirectory => ProgramRootDirectory + "/nvram";

        /// <summary>
        ///    The app instance directory for the program e.g nvram/app_01
        /// </summary>
        public static string ProgramNvramAppInstanceDirectory
        {
            get
            {
                if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server) return ProgramNvramDirectory;

                var path = ProgramNvramDirectory + "/app_" +
                           InitialParametersClass.ApplicationNumber
                               .ToString("D2");
                if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);

                return path;
            }
        }

        /// <summary>
        ///   The HTML directory for the processor or program instance on a server
        /// </summary>
        public static string ProgramHtmlDirectory => ProgramRootDirectory + "/html";

        /// <summary>
        ///   The base cws URL for the control system
        ///   <example>
        ///   https://vc4/VirtualControl/Rooms/ROOMAPPID/cws
        ///   </example>
        /// </summary>
        public static string CwsBaseUrl
        {
            get
            {
                if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)
                    return $"https://{IpAddress}/VirtualControl/Rooms/{InitialParametersClass.RoomId}/cws";

                return $"https://{IpAddress}/cws";
            }
        }

        /// <summary>
        ///  The date and time the program was built
        /// </summary>
        public DateTime ProgramBuildTime { get; }

        /// <summary>
        ///  The target framework the app was built for
        /// </summary>
        public string TargetFramework { get; }

        /// <summary>
        /// The Include4.dat version
        /// </summary>
        public string Include4DatInfo { get; }

        public static string CwsPath => CrestronEnvironment.DevicePlatform == eDevicePlatform.Server
            ? $"/VirtualControl/Rooms/{InitialParametersClass.RoomId}/cws"
            : "/cws";

        /// <summary>
        ///     Default URL for control system appliances to redirect.
        ///     Default value returns "/cws/app" for the cws web dashboard
        /// </summary>
        protected virtual string ApplianceWebServerRedirect => "/cws/app";

        /// <summary>
        ///   The boot status of the app
        /// </summary>
        public EBootStatus BootStatus { get; private set; }

        public EUnits LocalUnits { get; } = EUnits.Metric;

        /// <summary>
        ///  The description of the boot status
        /// </summary>
        public string BootStatusDescription { get; private set; }

        /// <summary>
        /// The progress of the boot status in percentage 0-100
        /// </summary>
        public uint BootProgress { get; private set; }

        /// <summary>
        ///     Set this to true when you create the system if using Fusion so it registers everything needed for it
        /// </summary>
        protected bool UseFusion { get; set; }

        /// <summary>
        ///   Get a device by its ID
        /// </summary>
        /// <param name="id">Unique number of the device</param>
        /// <returns>The associated <see cref="IDevice"/></returns>
        public IDevice GetDevice(uint id)
        {
            return DevicesDict[id];
        }

        /// <summary>
        ///   Get all devices in the system that conform to the IDevice interface
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IDevice> GetDevices()
        {
            return DevicesDict.Values;
        }

        internal IEnumerable<DisplayDeviceBase> GetDisplayDevices()
        {
            return GetDevices().OfType<DisplayDeviceBase>();
        }

        private void InitWebApp()
        {
            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)
                try
                {
                    var path = ProgramApplicationDirectory + "/webapp/index.html";
                    if (File.Exists(path))
                    {
                        var contents = File.ReadAllText(path);
                        if (Regex.IsMatch(contents, @"<base href=""/cws/app/"">"))
                        {
                            var baseHref = $"{CwsPath}/app/";
                            Logger.Warn($"Replacing base href value in \"{path}\" to \"{baseHref}\"");
                            contents = Regex.Replace(contents, @"<base href="".*"">",
                                $"<base href=\"{baseHref}\"");
                            File.WriteAllText(path, contents);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

            Logger.Highlight("Loading WebApp server for Angular app");
            try
            {
                WebAppServer = new WebScriptingServer(this, "app");
                if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance)
                    WebAppServer.AddRedirect(@"/app", @"/cws/app/");
                else
                    WebAppServer.AddRoute(@"/app", typeof(WebAppFileHandler));

                WebAppServer.AddRoute(@"/app/", typeof(WebAppFileHandler));
                WebAppServer.AddRoute(@"/app/<filepath:[~\/\w\.\-\[\]\(\)\x20]+>", typeof(WebAppFileHandler));
            }
            catch (Exception e)
            {
                Logger.Error("Could not load angular app web scripting server, {0}", e.Message);
                return;
            }

            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance)
            {
                Logger.Log("Device is appliance, Creating default index redirect to cws app");
                try
                {
                    var path = ProgramHtmlDirectory + "/index.html";
                    using (var file = File.CreateText(path))
                    {
                        file.Write($"<meta http-equiv=\"refresh\" content=\"0; URL={ApplianceWebServerRedirect}\" />");
                    }

                    Logger.Log($"Created file: \"{path}\"");

                    path = ProgramHtmlDirectory + "/_config_ini_";
                    using (var file = File.CreateText(path))
                    {
                        file.Write(@"webdefault=index.html");
                    }

                    Logger.Log($"Created file: \"{path}\"");

                    var response = string.Empty;
                    CrestronConsole.SendControlSystemCommand("webinit", ref response);
                    Logger.Highlight($"webinit response: {response}");
                }
                catch (Exception e)
                {
                    Logger.Error($"Error trying to init web server index, {e.Message}");
                }
            }
        }

        protected void UpdateBootStatus(EBootStatus status, string description, uint progress)
        {
            BootStatus = status;
            BootStatusDescription = description;
            BootProgress = progress;
            Logger.Debug("Boot status set to {0}, {1} ({2}%)", status, description, progress);
            EventService.Notify(EventMessageType.BootStatus, new
            {
                status,
                description,
                progress
            });
        }

        private void AppShouldRunUpgradeScriptsInternal()
        {
            AppShouldRunUpgradeScripts();
        }

        protected abstract void AppShouldRunUpgradeScripts();

        internal bool ConfigCheckIfRestartIsRequired(string configString)
        {
            return _initialConfig != configString;
        }

        /// <summary>
        ///     Initialise the cloud check-in connector for monitoring
        /// </summary>
        /// <param name="assembly">The app assembly</param>
        /// <param name="host">The hostname for the api connection</param>
        /// <param name="token">The token for the api connection</param>
        public void InitCloudConnector(Assembly assembly, string host, string token)
        {
            CloudConnector.Init(assembly, host, token);
            UpdateHelper.SetupUpdateTimer();
        }

        /// <summary>
        ///  Restart the app
        ///  <para>For VC-4 this will restart the app instance using the VC-4 server internal API</para>
        ///  <para>For Crestron hardware this will restart the app using the built in console command</para>
        /// </summary>
        /// <returns>The returned value of the command run</returns>
        public string RestartApp()
        {
            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)
                return Vc4WebApi.RestartApp().Result.ToString();

            var response = "";
            CrestronConsole.SendControlSystemCommand($"progres -P:{InitialParametersClass.ApplicationNumber}",
                ref response);
            return response;
        }

        /// <summary>
        ///   Reboot the appliance
        ///   <para>Only works on Crestron hardware, will throw an exception if called on a VC-4 server</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot reboot a VC-4 server</exception>
        public void RebootAppliance()
        {
            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)
                throw new InvalidOperationException("Cannot reboot server!");

            var response = string.Empty;
            Logger.Warn("Appliance now being rebooted!");
            UpdateBootStatus(EBootStatus.Rebooting, "Rebooting now!", 0);
            CrestronConsole.SendControlSystemCommand("reboot", ref response);
            Logger.Log("Reboot response: {0}", response);
        }

        private void SystemStoppingInternal(eProgramStatusEventType eventType)
        {
            if (eventType == eProgramStatusEventType.Stopping) Ch5WebSocketServer.Stop();

            try
            {
                OnProgramStatusEventHandler(eventType);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected abstract void OnProgramStatusEventHandler(eProgramStatusEventType eventType);

        internal IEnumerable<DiagnosticMessage> GenerateDiagnosticMessagesInternal()
        {
            var messages = new List<DiagnosticMessage>();
            if (Logger.ConsoleListening)
            {
                messages.Add(new DiagnosticMessage(MessageLevel.Info, "Logger console service running",
                    $"Running on TCP port {Logger.ListenPort}", GetType().Name));

                messages.AddRange(Logger.ConsoleConnections.Select(connection =>
                    new DiagnosticMessage(MessageLevel.Warning, $"Logger connection from {connection}",
                        "Console Connection", GetType().Name)));
            }

            if (Ch5WebSocketServer.Running)
                messages.Add(new DiagnosticMessage(MessageLevel.Info, "CH5 websocket service listening",
                    Ch5WebSocketServer.WebSocketBaseUrl, nameof(Ch5WebSocketServer)));

            messages.AddRange(CipDevices.GetDiagnosticMessages());

            foreach (var device in DevicesDict.Values) messages.AddRange(device.GetMessages());

            try
            {
                var handlers = Ch5ApiHandlerBase.ConnectedHandlers;
                messages.AddRange(handlers.Select(handler => new DiagnosticMessage(MessageLevel.Info,
                    "Websocket connected", handler.Connection.RemoteIpAddress.ToString(), handler.GetType().Name,
                    handler.GetType().Name)));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            try
            {
                messages.AddRange(GenerateDiagnosticMessages());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            var comparer = new SemiNumericComparer();

            return messages.OrderByDescending(m => m.Level).ThenBy(m => m.Message, comparer).ThenBy(m => m.DetailsMessage, comparer);
        }

        protected abstract IEnumerable<DiagnosticMessage> GenerateDiagnosticMessages();

        /// <summary>
        /// Initialize the system
        /// </summary>
        public void Initialize()
        {
            Logger.Highlight("Initialize()");
            UpdateBootStatus(EBootStatus.Initializing, "System Initializing", 0);

            Logger.Log("Starting system initialize task");
            var task = new Task(InitializeTask);
            task.ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    Logger.Success("System Initialized OK");
                    UpdateBootStatus(EBootStatus.Running, "System Running", 100);
                }
                else
                {
                    Logger.Warn("System initialize task ended and status is: {0}", t.Status);
                }
            });

            UpdateBootStatus(EBootStatus.Initializing, $"Starting {GetType().Name}.Initialize()", 2);
            task.Start();
        }

        private void InitializeTask()
        {
            UpdateBootStatus(EBootStatus.Initializing, "Initializing process started", 5);
            Thread.Sleep(500);

            UpdateBootStatus(EBootStatus.Initializing, "Initializing web app if installed", 7);
            InitWebApp();

            UpdateBootStatus(EBootStatus.Initializing, "Registering CIP devices not already registered", 10);
            CipDevices.RegisterDevices();

            Logger.Debug("WebScriptingHandlersShouldRegister()");
            try
            {
                WebScriptingHandlersShouldRegister();
            }
            catch (Exception e)
            {
                Logger.Error("Error registering app webscripting handlers, {0}", e.Message);
            }

            foreach (var device in DevicesDict.Values.OfType<DeviceBase>()) device.AllocateRoomOnStart();

            SystemShouldAddItemsToInitialize(AddItemToInitialize);

            var items = new List<IInitializable>();
            items.AddRange(DevicesDict.Values);
            items.AddRange(_itemsToInitialize);

            var startPercentage = BootProgress;
            var targetPercentage = 40U;
            var itemMaxCount = items.Count;
            var itemCount = 0;
            foreach (var item in items)
                try
                {
                    itemCount++;
                    UpdateBootStatus(EBootStatus.Initializing, "Initializing " + item.Name,
                        (uint)Tools.ScaleRange(itemCount, 0, itemMaxCount, startPercentage, targetPercentage));
                    Logger.Debug("Initializing {0}", item.ToString());
                    item.Initialize();
                    Thread.Sleep(50);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

            startPercentage = BootProgress;
            targetPercentage = 60;
            var rooms = UxEnvironment.GetRooms();
            itemMaxCount = rooms.Count;
            itemCount = 0;

            foreach (var room in rooms)
            {
                itemCount++;
                UpdateBootStatus(EBootStatus.Initializing, "Initializing " + room,
                    (uint)Tools.ScaleRange(itemCount, 0, itemMaxCount, startPercentage, targetPercentage));
                try
                {
                    Logger.Debug("Initializing room: {0}", room);
                    room.InternalInitialize();
                    Logger.Debug("Initialize room complete: {0}", room);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            UpdateBootStatus(EBootStatus.Initializing, "Initializing rooms done", targetPercentage);

            startPercentage = BootProgress;
            targetPercentage = 60;
            var sources = UxEnvironment.GetSources();
            itemMaxCount = sources.Count;
            itemCount = 0;

            foreach (var source in sources)
            {
                itemCount++;
                UpdateBootStatus(EBootStatus.Initializing, "Initializing " + source,
                    (uint)Tools.ScaleRange(itemCount, 0, itemMaxCount, startPercentage, targetPercentage));
                try
                {
                    Logger.Debug("Initializing source: {0}", source);
                    source.InternalInitialize();
                    Logger.Debug("Initialize source complete: {0}", source);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            UpdateBootStatus(EBootStatus.Initializing, "Initializing sources done", targetPercentage);


            if (UseFusion)
            {
                Thread.Sleep(200);
                UpdateBootStatus(EBootStatus.Initializing, "Setting up Fusion if required", 65);
                try
                {
                    CreateFusionRoomsAndAssets();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                Thread.Sleep(200);
                UpdateBootStatus(EBootStatus.Initializing, "Generating RVI file info for Fusion", 70);
                Logger.Debug("Generating Fusion RVI File");
                try
                {
                    FusionRVI.GenerateFileForAllFusionDevices();
                    Logger.Debug("Generated Fusion RVI file");
                    try
                    {
                        var dir = new DirectoryInfo(ProgramApplicationDirectory);
                        var rviFile = dir.GetFiles("*.rvi").FirstOrDefault();
                        if (rviFile != null)
                        {
                            Logger.Log($"RVI File found, checking contents to fix null endings, {rviFile.Name}");
                            var lines = File.ReadAllLines(rviFile.FullName);
                            File.Delete(rviFile.FullName);
                            var newLines = new List<string>();
                            foreach (var line in lines)
                            {
                                if (line.Contains("</RoomViewInfo>"))
                                {
                                    newLines.Add(line);
                                    break;
                                }

                                newLines.Add(line);
                            }

                            File.WriteAllLines(rviFile.FullName, newLines);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                Thread.Sleep(200);
                UpdateBootStatus(EBootStatus.Initializing, "Registering Fusion", 80);
                CipDevices.RegisterFusionRooms();
            }

            Thread.Sleep(500);
            UpdateBootStatus(EBootStatus.Initializing, "Starting CH5 websocket services", 90);
            Thread.Sleep(500);
            if (Ch5WebSocketServer.InitCalled)
            {
                Logger.Log("Starting CH5 websocket services");
                Thread.Sleep(1000);
                Ch5WebSocketServer.Start();
                Thread.Sleep(2000);
            }

            if (Core3Controllers.Count > 0)
            {
                UpdateBootStatus(EBootStatus.Initializing, "Initializing Core 3 UI Controllers", 95);
                Logger.Log("Initializing Core 3 UI Controllers");
                InitializeCore3Controllers();
            }

            try
            {
                OnInitializeComplete();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw;
            }
        }

        protected virtual void OnInitializeComplete()
        {
            // optional complete init method
        }

        /// <summary>
        ///     Add any <c>IInitializable</c> items to the startup routine.
        /// </summary>
        /// <returns></returns>
        protected abstract void SystemShouldAddItemsToInitialize(Action<IInitializable> addItem);

        /// <summary>
        ///     This is now optional as virtual rather than abstract.
        /// </summary>
        protected virtual void CreateFusionRoomsAndAssets()
        {
            // optional fusion method
        }

        private void AddItemToInitialize(IInitializable itemToInitialize)
        {
            if (_itemsToInitialize.Contains(itemToInitialize))
            {
                Logger.Warn("Item already registered to initialize, {0}", itemToInitialize.Name);
                return;
            }

            if (DevicesDict.Values.Contains(itemToInitialize))
            {
                Logger.Warn("Item already registered to initialize, {0}", itemToInitialize.Name);
                return;
            }

            _itemsToInitialize.Add(itemToInitialize);
        }

        /// <summary>
        ///     Register webscripting handlers specific to the room app
        /// </summary>
        protected abstract void WebScriptingHandlersShouldRegister();

        private bool CheckIfNewVersion(Assembly appAssembly)
        {
            try
            {
                var runningVersion = appAssembly.GetName().Version;

                Logger.Log("Checking version of {0} to see if \"{1}\" is new", appAssembly.GetName().Name,
                    runningVersion);

                var filePath = $"{ProgramNvramAppInstanceDirectory}/{appAssembly.GetName().Name}_version.info";

                if (!File.Exists(filePath))
                {
                    using (var newFile = File.OpenWrite(filePath))
                    {
                        var bytes = Encoding.UTF8.GetBytes(runningVersion.ToString());
                        newFile.Write(bytes, 0, bytes.Length);
                    }

                    Logger.Log("Version file created at \"{0}\", app must be updated or new", filePath);
                    return true;
                }

                bool appIsNewVersion;

                using (var file = new StreamReader(filePath, Encoding.UTF8))
                {
                    var contents = file.ReadToEnd().Trim();
                    var version = new Version(contents);
                    appIsNewVersion = runningVersion.CompareTo(version) != 0;
                    if (appIsNewVersion)
                        Logger.Log("App upgraded {0} => {1}", version, runningVersion);
                    else
                        Logger.Log("App version remains as {0}", runningVersion.ToString());
                }

                if (!appIsNewVersion) return false;
                {
                    File.Delete(filePath);

                    using (var newFile = File.OpenWrite(filePath))
                    {
                        var bytes = Encoding.UTF8.GetBytes(runningVersion.ToString());
                        newFile.Write(bytes, 0, bytes.Length);
                    }

                    Logger.Highlight("Version file deleted and created at \"{0}\", with new version number", filePath);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Error checking if app is new version, returning true, {0}", e.Message);
                return true;
            }
        }

        private void InitializeCore3Controllers()
        {
            foreach (var controller in Core3Controllers.Get())
                try
                {
                    controller.InitializeInternal();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
        }

        internal void RunCloudActionInternal(string methodName, Dictionary<string, string> args)
        {
            switch (methodName)
            {
                case "restart":
                    Logger.Warn("Remote restart requested from cloud service");

                    Task.Run(() =>
                    {
                        Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                        UxEnvironment.System.RestartApp();
                    });
                    break;
                case "reboot":
                    Logger.Warn("Remote reboot requested from cloud service");
                    Task.Run(() =>
                    {
                        Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                        UxEnvironment.System.RebootAppliance();
                    });
                    break;
                case "uploadLogs":
                    CloudConnector.PublishLogsAsync();
                    break;
                case "update":
                    if (args.TryGetValue("fileName", out var fileName))
                        UpdateHelper.UpdateRunningProgram(fileName);
                    else
                        throw new ArgumentException("No update file specified");
                    break;
            }
        }
    }
}