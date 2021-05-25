using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
using UXAV.AVnet.Core.WebScripting;
using UXAV.AVnet.Core.WebScripting.Download;
using UXAV.AVnet.Core.WebScripting.InternalApi;
using UXAV.AVnet.Core.WebScripting.StaticFiles;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Models
{
    public abstract class SystemBase
    {
        private static string _programRootDirectory;
        private static string _systemName;
        private EBootStatus _bootStatus;
        private string _bootStatusDescription;
        private uint _bootProgress;
        private EUnits _localUnits = EUnits.Metric;
        private readonly string _initialConfig;
        internal readonly Dictionary<uint, IDevice> DevicesDict = new Dictionary<uint, IDevice>();
        private readonly List<IInitializable> _itemsToInitialize = new List<IInitializable>();
        private readonly string _include4DatInfo;
        private readonly DateTime _programBuildTime;

        protected SystemBase(CrestronControlSystem controlSystem)
        {
            Logger.MessageLogged += message => { EventService.Notify(EventMessageType.LogEntry, message); };

            Logger.Highlight("{0}.ctor()", GetType().FullName);

            Logger.Highlight("System.ctor()");

            UxEnvironment.System = this;
            UxEnvironment.ControlSystem = controlSystem;
            UxEnvironment.InitConsoleCommands();
            CipDevices.Init(controlSystem);

            _initialConfig = ConfigManager.JConfig?.ToString();

            DiagnosticService.RegisterSystemCallback(this, GenerateDiagnosticMessagesInternal);

            CrestronEnvironment.ProgramStatusEventHandler += OnProgramStatusEventHandler;

            RoomClock.Start();
            UpdateBootStatus(EBootStatus.Booting, "System is booting", 0);

            try
            {
                var infoFile = ProgramApplicationDirectory + "/ProgramInfo.config";
                using (var file = File.OpenRead(infoFile))
                {
                    var reader = new XmlTextReader(file);
                    var elementName = string.Empty;
                    while (reader.Read())
                    {
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
                                    case "Include4.dat":
                                        _include4DatInfo = reader.Value;
                                        break;
                                    case "CompiledOn":
                                        _programBuildTime = DateTime.Parse(reader.Value);
                                        break;
                                }

                                break;
                        }
                    }

                    reader.Close();
                }
            }
            catch (Exception e)
            {
                Logger.Warn($"Could not load info from ProgramInfo.config, {e.Message}");
            }

            SystemMonitor.Init();
            try
            {
                Logger.Highlight("Calling CrestronDataStoreStatic.InitCrestronDataStore()");
                var response = CrestronDataStoreStatic.InitCrestronDataStore();
                if (response == CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
                {
                    Logger.Success($"InitCrestronDataStore() = {response}");
                }
                else
                {
                    Logger.Error($"CrestronDataStore Init Error: {response}");
                }
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
            Logger.Log("Include4.dat Version: {0}", _include4DatInfo);
            Logger.Log("Local Time is {0}", DateTime.Now);
            var tz = CrestronEnvironment.GetTimeZone();
            Logger.Log("ProgramIDTag: {0}", InitialParametersClass.ProgramIDTag);
            Logger.Log("ApplicationNumber: {0}", InitialParametersClass.ApplicationNumber);
            Logger.Log("FirmwareVersion: {0}", InitialParametersClass.FirmwareVersion);
            Logger.Log("SerialNumber: {0}", CrestronEnvironment.SystemInfo.SerialNumber);
            Logger.Log("App Info: {0}", AppAssembly.GetName().FullName);
            Logger.Log("{0} Version: {1}", UxEnvironment.Name, UxEnvironment.Version);
            Logger.Log("{0} Assembly Version: {1}", UxEnvironment.Name, UxEnvironment.AssemblyVersion);
            Logger.Log("Starting app version {0}", AppAssembly.GetName().Version);
            Logger.Log($"Program Info states build time as: {_programBuildTime:R}");
            Logger.Log("ProcessId: {0}", Process.GetCurrentProcess().Id);
            Logger.Log("Room Name: {0}", InitialParametersClass.RoomName);
            Logger.Log("TimeZone: 🌍 {0}{1}", tz.Formatted, tz.InDayLightSavings ? " (DST)" : string.Empty);
            Logger.Log("ProgramRootDirectory = {0}", ProgramRootDirectory);
            Logger.Log("ProgramUserDirectory = {0}", ProgramUserDirectory);
            Logger.Log("ProgramNvramDirectory = {0}", ProgramNvramDirectory);
            Logger.Log("ProgramHtmlDirectory = {0}", ProgramHtmlDirectory);
            Logger.Log("DevicePlatform = {0}", CrestronEnvironment.DevicePlatform);
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

            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (var resource in resources)
            {
                Logger.Log("Resource found: {0}", resource);
            }

            UpdateBootStatus(EBootStatus.Booting, "Checking upgrade requirements", 0);
            var appIsUpgrading = CheckIfNewVersion(AppAssembly);
            Logger.Warn("App is new version: {0}", appIsUpgrading);
            if (appIsUpgrading)
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
                ApiServer.AddRoute(@"/api/appcontrol", typeof(AppControlApiHandler));
                ApiServer.AddRoute(@"/api/autodiscovery", typeof(AutoDiscoveryApiHandler));
                ApiServer.AddRoute(@"/api/console", typeof(ConsoleApiHandler));
                ApiServer.AddRoute(@"/api/diagnostics", typeof(DiagnosticsApiHandler));
                ApiServer.AddRoute(@"/api/sysmon", typeof(SystemMonitorApiHandler));
                ApiServer.AddRoute(@"/api/upload/<fileType:\w+>", typeof(FileUploadApiHandler));
                ApiServer.AddRoute(@"/api/upload/uploadedfiles/<fileType:\w+>", typeof(UploadedFilesApiHandler));
                ApiServer.AddRoute(@"/api/xpanels", typeof(XPanelDetailsApiHandler));
            }
            catch (Exception e)
            {
                Logger.Warn("Could not load API web scripting server, {0}", e.Message);
            }

            Logger.Highlight("Loading WebApp server for Angular app");
            try
            {
                WebAppServer = new WebScriptingServer(this, "app");
                WebAppServer.AddRedirect(@"/app", @"/cws/app/");
                WebAppServer.AddRoute(@"/app/", typeof(WebAppFileHandler));
                WebAppServer.AddRoute(@"/app/<filepath:[~\/\w\.\-\[\]\(\)\x20]+>", typeof(WebAppFileHandler));
            }
            catch (Exception e)
            {
                Logger.Error("Could not load angular app web scripting server, {0}", e.Message);
                UpdateBootStatus(EBootStatus.DidNotBoot, $"{GetType().Name}.CTOR FAIL, {e.Message}", 0);
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
                        file.Write(@"<meta http-equiv=""refresh"" content=""0; URL=/cws/app"" />");
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

            // Wait for above handlers to start accepting requests and update.
            Thread.Sleep(1000);
            Logger.Success(".ctor() Complete", true);
            UpdateBootStatus(EBootStatus.Booting, "Waiting to initialize...", 0);
        }

        public CrestronControlSystem ControlSystem { get; }

        protected WebScriptingServer FileServer { get; }

        protected WebScriptingServer ApiServer { get; }

        protected WebScriptingServer WebAppServer { get; }

        internal static Assembly AppAssembly { get; private set; }

        public static string SystemName
        {
            get => string.IsNullOrEmpty(_systemName) ? InitialParametersClass.RoomName : _systemName;
            set => _systemName = value;
        }

        public static DateTime BootTime { get; } = DateTime.Now;

        public static TimeSpan UpTime => DateTime.Now - BootTime;

        public static string HostName =>
            CrestronEthernetHelper.GetEthernetParameter(
                CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_HOSTNAME,
                CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType
                    .EthernetLANAdapter));

        public static string DomainName =>
            CrestronEthernetHelper.GetEthernetParameter(
                CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_DOMAIN_NAME,
                CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType
                    .EthernetLANAdapter));

        public static string DhcpStatus =>
            CrestronEthernetHelper.GetEthernetParameter(
                CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_STARTUP_DHCP_STATUS,
                CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType
                    .EthernetLANAdapter));

        public static string IpAddress =>
            CrestronEthernetHelper.GetEthernetParameter(
                CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS,
                CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType
                    .EthernetLANAdapter));

        public static string MacAddress =>
            CrestronEthernetHelper.GetEthernetParameter(
                CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_MAC_ADDRESS,
                CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType
                    .EthernetLANAdapter));

        public virtual string AppName => AppAssembly.GetName().Name;

        public virtual Version AppVersion
        {
            get
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(AppAssembly.Location);
                return new Version(fileVersionInfo.ProductVersion);
            }
        }

        public virtual Version AppAssemblyVersion => AppAssembly.GetName().Version;

        public static eDevicePlatform DevicePlatform => CrestronEnvironment.DevicePlatform;

        public static string ProgramRootDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_programRootDirectory))
                {
                    _programRootDirectory = Crestron.SimplSharp.CrestronIO.Directory.GetApplicationRootDirectory();
                }

                return _programRootDirectory;
            }
        }

        public static string ProgramApplicationDirectory => InitialParametersClass.ProgramDirectory.ToString();

        public static string TempFileDirectory
        {
            get
            {
                var path = ProgramNvramDirectory + "/tmp";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        public static string ProgramUserDirectory => ProgramRootDirectory + "/user";

        public static string ProgramNvramDirectory => ProgramRootDirectory + "/nvram";

        /// <summary>
        /// The app instance directory for the program e.g nvram/app_01
        /// </summary>
        public static string ProgramNvramAppInstanceDirectory
        {
            get
            {
                var path = ProgramNvramDirectory + "/app_" +
                           InitialParametersClass.ApplicationNumber
                               .ToString("D2");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        public static string ProgramHtmlDirectory => ProgramRootDirectory + "/html";

        public DateTime ProgramBuildTime => _programBuildTime;

        public string Include4DatInfo => _include4DatInfo;

        public IDevice GetDevice(uint id)
        {
            return DevicesDict[id];
        }

        public IEnumerable<IDevice> GetDevices()
        {
            return DevicesDict.Values;
        }

        internal IEnumerable<DisplayDeviceBase> GetDisplayDevices()
        {
            return GetDevices().OfType<DisplayDeviceBase>();
        }

        public enum EBootStatus
        {
            Booting,
            LoadingConfig,
            Initializing,
            Running,
            DidNotBoot,
            Rebooting
        }

        public EBootStatus BootStatus
        {
            get => _bootStatus;
        }

        public enum EUnits
        {
            Imperial,
            Metric
        }

        public EUnits LocalUnits
        {
            get => _localUnits;
        }

        public string BootStatusDescription
        {
            get => _bootStatusDescription;
        }

        public uint BootProgress => _bootProgress;

        protected void UpdateBootStatus(EBootStatus status, string description, uint progress)
        {
            _bootStatus = status;
            _bootStatusDescription = description;
            _bootProgress = progress;
            Logger.Log("Boot status set to {0}, {1} ({2}%)", status, description, progress);
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

        public void InitCloudConnector(Assembly assembly, string token)
        {
            CloudConnector.Init(assembly, token);
        }

        public void RebootAppliance()
        {
            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)
            {
                throw new InvalidOperationException("Cannot reboot server!");
            }

            var response = string.Empty;
            Logger.Warn("Appliance now being rebooted!");
            UpdateBootStatus(EBootStatus.Rebooting, "Rebooting now!", 0);
            CrestronConsole.SendControlSystemCommand("reboot", ref response);
            Logger.Log("Reboot response: {0}", response);
        }

        protected abstract void OnProgramStatusEventHandler(eProgramStatusEventType eventType);

        private IEnumerable<DiagnosticMessage> GenerateDiagnosticMessagesInternal()
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

            messages.AddRange(CipDevices.GetDiagnosticMessages());

            foreach (var device in DevicesDict.Values)
            {
                messages.AddRange(device.GetMessages());
            }

            try
            {
                messages.AddRange(GenerateDiagnosticMessages());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return messages.OrderByDescending(m => m.Level);
        }

        protected abstract IEnumerable<DiagnosticMessage> GenerateDiagnosticMessages();

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
            UpdateBootStatus(EBootStatus.Initializing, $"Initializing process started", 5);
            Thread.Sleep(500);

            UpdateBootStatus(EBootStatus.Initializing, "Registering CIP devices not already registered", 10);
            CipDevices.RegisterDevices();

            Logger.Highlight("WebScriptingHandlersShouldRegister()");
            try
            {
                WebScriptingHandlersShouldRegister();
            }
            catch (Exception e)
            {
                Logger.Error("Error registering app webscripting handlers, {0}", e.Message);
            }

            foreach (var device in DevicesDict.Values.OfType<DeviceBase>())
            {
                device.AllocateRoomOnStart();
            }

            SystemShouldAddItemsToInitialize(AddItemToInitialize);

            var items = new List<IInitializable>();
            items.AddRange(DevicesDict.Values);
            items.AddRange(_itemsToInitialize);

            var startPercentage = BootProgress;
            var targetPercentage = 40U;
            var itemMaxCount = items.Count;
            var itemCount = 0;
            foreach (var item in items)
            {
                try
                {
                    itemCount++;
                    UpdateBootStatus(EBootStatus.Initializing, "Initializing " + item.Name,
                        (uint) Tools.ScaleRange(itemCount, 0, itemMaxCount, startPercentage, targetPercentage));
                    Logger.Highlight("Initializing {0}", item.ToString());
                    item.Initialize();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
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
                    (uint) Tools.ScaleRange(itemCount, 0, itemMaxCount, startPercentage, targetPercentage));
                Logger.Highlight("Initializing room: {0}", room);
                room.InternalInitialize();
            }

            UpdateBootStatus(EBootStatus.Initializing, "Initializing rooms done", targetPercentage);

            startPercentage = BootProgress;
            targetPercentage = 80;
            var sources = UxEnvironment.GetSources();
            itemMaxCount = sources.Count;
            itemCount = 0;

            foreach (var source in sources)
            {
                itemCount++;
                UpdateBootStatus(EBootStatus.Initializing, "Initializing " + source,
                    (uint) Tools.ScaleRange(itemCount, 0, itemMaxCount, startPercentage, targetPercentage));
                Logger.Highlight("Initializing source: {0}", source);
                source.InternalInitialize();
            }

            UpdateBootStatus(EBootStatus.Initializing, "Initializing sources done", targetPercentage);
            Thread.Sleep(200);
            UpdateBootStatus(EBootStatus.Initializing, "Setting up Fusion if required", 82);
            try
            {
                CreateFusionRoomsAndAssets();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            Thread.Sleep(200);
            UpdateBootStatus(EBootStatus.Initializing, "Generating RVI file info for Fusion", 85);
            Logger.Highlight("Generating Fusion RVI File");
            try
            {
                FusionRVI.GenerateFileForAllFusionDevices();
                Logger.Success("Generated Fusion RVI file");
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
            UpdateBootStatus(EBootStatus.Initializing, "Registering Fusion", 90);
            CipDevices.RegisterFusionRooms();

            Thread.Sleep(200);
            UpdateBootStatus(EBootStatus.Initializing, "Initializing Core 3 UI Controllers", 95);
            Thread.Sleep(200);
            Logger.Highlight("Initializing Core 3 UI Controllers");
            InitializeCore3Controllers();
            Thread.Sleep(200);
        }

        /// <summary>
        /// Add any <c>IInitializable</c> items to the startup routine.
        /// </summary>
        /// <returns></returns>
        ///
        protected abstract void SystemShouldAddItemsToInitialize(Action<IInitializable> addItem);

        protected abstract void CreateFusionRoomsAndAssets();

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
        /// Register webscripting handlers specific to the room app
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
                    {
                        Logger.Log("App upgraded {0} => {1}", version, runningVersion);
                    }
                    else
                    {
                        Logger.Log("App version remains as {0}", runningVersion.ToString());
                    }
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
            {
                try
                {
                    controller.InitializeInternal();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }
    }
}