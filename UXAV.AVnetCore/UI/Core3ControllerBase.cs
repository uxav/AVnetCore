using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Rooms;
using UXAV.AVnetCore.Models.Sources;
using UXAV.AVnetCore.UI.Components;
using UXAV.AVnetCore.UI.Components.Views;
using UXAV.AVnetCore.UI.ReservedJoins;
using UXAV.Logging;
using IButton = UXAV.AVnetCore.UI.Components.IButton;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace UXAV.AVnetCore.UI
{
    public abstract class Core3ControllerBase : ISigProvider, IGenericDeviceWrapper
    {
        private readonly uint _roomId;
        private RoomBase _room;
        private RoomBase _defaultRoom;

        private readonly Dictionary<DeviceExtender, string> _deviceExtenderNames =
            new Dictionary<DeviceExtender, string>();

        protected Core3ControllerBase(SystemBase system, uint roomId, string typeName, uint ipId, string description)
        {
            _roomId = roomId;
            System = system;
            Logger.Highlight(
                $"Creating {GetType().FullName} with device type {typeName} with IP ID: {ipId:X2}");

            Device = (BasicTriListWithSmartObject) CipDevices.CreateDevice(typeName, ipId, description);

            SigProvider = new SigProviderDevice(Device);

            Device.Description = description;
            Core3Controllers.Add(ipId, this);

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                Device.BooleanInput[10].BoolValue = type == eProgramStatusEventType.Stopping;
            };
            Device.IpInformationChange += OnIpInformationChange;
            Device.OnlineStatusChange += OnOnlineStatusChange;

            Device.StringInput[Serial.RoomNameOrHostname].StringValue =
                SystemBase.DevicePlatform == eDevicePlatform.Server
                    ? InitialParametersClass.RoomName
                    : SystemBase.HostName;
            Device.StringInput[Serial.AppRoomId].StringValue = InitialParametersClass.RoomId;
            Device.StringInput[Serial.AppNumber].StringValue =
                $"App Number: {InitialParametersClass.ApplicationNumber}";
            Device.StringInput[Serial.AppVersion].StringValue = $"{System.AppName} v{system.AppVersion}";
            Device.StringInput[Serial.TimeZoneFormatted].StringValue =
                $"TimeZone: {CrestronEnvironment.GetTimeZone().Formatted}";
            Device.StringInput[Serial.TimeZoneCoordinates].StringValue =
                $"Coord:{CrestronEnvironment.Latitude},{CrestronEnvironment.Longitude}";
            Device.StringInput[Serial.LocalUnits].StringValue = System.LocalUnits.ToString();
            Device.StringInput[Serial.TimeZone].StringValue = $"Time Zone: {CrestronEnvironment.GetTimeZone()}";
            Device.StringInput[Serial.PanelMacAddress].StringValue = "Unknown";
            SigProvider.StringInput[Serial.ProcessorHostName].StringValue = SystemBase.HostName;
            SigProvider.StringInput[Serial.ProcessorIpAddress].StringValue = SystemBase.IpAddress;

            if (SystemBase.DevicePlatform == eDevicePlatform.Server)
            {
                //var programInstanceData = Vc4WebApi.GetProgramInstance(InitialParametersClass.RoomId);
                //Device.StringInput[Serial.RoomLocation].StringValue = $"Location: {programInstanceData["Location"]}";
            }

            ExtenderAuthentication = UseDeviceExtenderByName("ExtenderAuthenticationReservedSigs");
            ExtenderAutoUpdate = UseDeviceExtenderByName("ExtenderAutoUpdateReservedSigs");
            ExtenderCamera = UseDeviceExtenderByName("ExtenderCameraReservedSigs");
            ExtenderEthernet = UseDeviceExtenderByName("ExtenderEthernetReservedSigs");
            //ExtenderApplication = UseDeviceExtenderByName("ExtenderApplicationControlReservedSigs");
            ExtenderZoomRoom = UseDeviceExtenderByName("ExtenderZoomRoomAppReservedSigs");
            ExtenderHardButton = UseDeviceExtenderByName("ExtenderHardButtonReservedSigs");
            ExtenderHardButton?.SetUShortPropertyValue("Brightness", ushort.MaxValue);
            ExtenderHardButton?.InvokeMethod("DisableBrightnessAutoOn");
            ExtenderAudio = UseDeviceExtenderByName("ExtenderAudioReservedSigs");
            ExtenderSystem = UseDeviceExtenderByName("ExtenderSystemReservedSigs");
            ExtenderSystem?.InvokeMethod("LcdBrightnessAutomaticOff");
            ExtenderSystem?.SetUShortPropertyValue("LcdBrightness", ushort.MaxValue);
            ExtenderSystem2 = UseDeviceExtenderByName("ExtenderSystem2ReservedSigs");
            ExtenderSystem3 = UseDeviceExtenderByName("ExtenderSystem3ReservedSigs");
            ExtenderSystem4 = UseDeviceExtenderByName("ExtenderSystem4ReservedSigs");
            ExtenderUsbLedAccessory = UseDeviceExtenderByName("ExtenderUsbLedAccessoryControlReservedSigs");
            ExtenderIntegratedLightBar = UseDeviceExtenderByName("ExtenderIntegratedLightBarReservedSigs");
            ExtenderScreenSaver = UseDeviceExtenderByName("ExtenderScreenSaverReservedSigs");
            ExtenderTouchDetection = UseDeviceExtenderByName("ExtenderTouchDetectionReservedSigs");
            ExtenderCrestronAppFunctions = UseDeviceExtenderByName("ExtenderCrestronAppFunctions");
            ExtenderPinPoint = UseDeviceExtenderByName("ExtenderPinPointReservedSigs");
            ExtenderSetup = UseDeviceExtenderByName("ExtenderSetupReservedSigs");

            Pages = new UIPageCollection(this);

            HardButtons = new UIButtonCollection();
            for (var button = 1U; button <= 5; button++)
            {
                HardButtons.Add(new UIHardButton(this, button, ExtenderHardButton));
            }

            HardButtons.ButtonEvent += OnHardButtonEvent;

            var files = Directory.GetFiles(SystemBase.ProgramApplicationDirectory,
                "*.sgd", SearchOption.AllDirectories);
            // Look for SGD files with priority given to file names containing the device type name... ie 'CrestronApp'
            foreach (var file in files.OrderByDescending(f => f.Contains(Device.GetType().Name)))
            {
                Logger.Success("Found SGD File: {0}, Loading...", file);
                try
                {
                    using (var fileStream = File.OpenRead(file))
                    {
                        Device.LoadSmartObjects(fileStream.GetCrestronStream());
                    }

                    foreach (var smartObject in Device.SmartObjects)
                    {
                        Logger.Debug($"{this} has SmartObject ID {smartObject.Key}");
                    }

                    break;
                }
                catch (Exception e)
                {
                    Logger.Error($"Error loading SGD file, {e.Message}");
                }
            }
        }

        protected DeviceExtender UseDeviceExtenderByName(string name)
        {
            try
            {
                var extender = (DeviceExtender) Device.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => p.Name == name)
                    ?.GetValue(Device, null);
                if (extender != null && !_deviceExtenderNames.ContainsKey(extender))
                {
                    extender.Use();
                    extender.DeviceExtenderSigChange += OnDeviceExtenderSigChange;
                    Logger.Debug($"{this} has {extender.GetType().Name} \"{name}\"");
                    _deviceExtenderNames[extender] = name;
                    return extender;
                }
            }
            catch (Exception e)
            {
                Logger.Warn(
                    $"Could not get {name} Device Extender from {Device.GetType().FullName}, {e.Message}");
            }

            return null;
        }

        public BasicTriListWithSmartObject Device { get; }

        public GenericDevice GenericDevice => Device;

        public event RoomChangeEventHandler RoomChanged;

        public uint Id => Device.ID;

        public SigProviderDevice SigProvider { get; }

        public SystemBase System { get; }

        public UIPageCollection Pages { get; }

        public string IpAddress
        {
            get => SigProvider.StringInput[Serial.PanelIpAddress].StringValue;
            set => SigProvider.StringInput[Serial.PanelIpAddress].StringValue = value;
        }

        public string MacAddress
        {
            get => SigProvider.StringInput[Serial.PanelMacAddress].StringValue;
            set => SigProvider.StringInput[Serial.PanelMacAddress].StringValue = value;
        }

        public UIButtonCollection HardButtons { get; }

        protected DeviceExtender ExtenderAuthentication { get; }

        protected DeviceExtender ExtenderHardButton { get; }

        protected DeviceExtender ExtenderAudio { get; }

        protected DeviceExtender ExtenderEthernet { get; }

        protected DeviceExtender ExtenderCamera { get; }

        protected DeviceExtender ExtenderApplication { get; }

        protected DeviceExtender ExtenderZoomRoom { get; }

        protected DeviceExtender ExtenderSystem { get; }

        protected DeviceExtender ExtenderSystem2 { get; }

        protected DeviceExtender ExtenderSystem3 { get; }

        protected DeviceExtender ExtenderSystem4 { get; }

        protected DeviceExtender ExtenderUsbLedAccessory { get; }

        protected DeviceExtender ExtenderIntegratedLightBar { get; }

        protected DeviceExtender ExtenderScreenSaver { get; }

        protected DeviceExtender ExtenderTouchDetection { get; }

        protected DeviceExtender ExtenderAutoUpdate { get; }

        public DeviceExtender ExtenderSetup { get; }

        public DeviceExtender ExtenderPinPoint { get; }

        public DeviceExtender ExtenderCrestronAppFunctions { get; }

        protected string AppProjectName
        {
            get => !(Device is CrestronAppBase device) ? string.Empty : device.ParameterProjectName.Value;
            set
            {
                if (Device is CrestronAppBase device) device.ParameterProjectName.Value = value;
            }
        }

        private void OnIpInformationChange(GenericBase currentdevice, ConnectedIpEventArgs args)
        {
            if (args.Connected)
            {
                Logger.Log($"{currentdevice} has connected with IP: " + args.DeviceIpAddress);
                IpAddress = args.DeviceIpAddress;
                if (ExtenderSystem == null) return;
                Logger.Debug($"{Device} Turning Backlight On");
                ExtenderSystem.InvokeMethod("BacklightOn");
            }
            else
            {
                Logger.Log($"{currentdevice} has disconnected with IP: " + args.DeviceIpAddress);
            }
        }

        protected virtual void OnOnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            if (args.DeviceOnLine)
            {
                Logger.Success($"{currentDevice} is now online");
            }
            else
            {
                Logger.Warn($"{currentDevice} is now offline");
            }
        }

        protected void OnDeviceExtenderSigChange(DeviceExtender extender, SigEventArgs args)
        {
            var extenderName = _deviceExtenderNames.ContainsKey(extender)
                ? _deviceExtenderNames[extender]
                : extender.GetType().Name;
            var sigName = extender.GetSigPropertyName(args.Sig);
            if (string.IsNullOrEmpty(sigName)) return;
            if (sigName != "LightSensorValueFeedback")
            {
                Logger.Debug($"{Device} {extenderName}.{sigName} = {args.Sig}");
            }

            switch (sigName)
            {
                case "IpAddressFeedback":
                    Logger.Log($"{this} IP Address changed: {args.Sig.StringValue}");
                    IpAddress = args.Sig.StringValue;
                    break;
                case "MacAddressFeedback":
                    Logger.Log($"{this} MAC Address changed: {args.Sig.StringValue}");
                    MacAddress = args.Sig.StringValue;
                    break;
                case "LightSensorValueFeedback":
                    OnLightSensorValueChanged(args.Sig.UShortValue);
                    break;
            }
        }

        public RoomBase DefaultRoom => _defaultRoom;

        public RoomBase Room
        {
            get => _room;
            set
            {
                if (_room == value) return;
                if (_room != null)
                {
                    _room.SourceChanged -= RoomOnSourceChangedInternal;
                }

                _room = value;
                OnRoomChangeInternal(value);
                if (_room != null)
                {
                    _room.SourceChanged += RoomOnSourceChangedInternal;
                }
            }
        }

        private void OnRoomChangeInternal(RoomBase value)
        {
            Logger.Highlight($"{this} Room Set: {value}");
            try
            {
                OnRoomChange(value);
                RoomChanged?.Invoke(this, value);
                if (value.GetCurrentSource() != null)
                {
                    UIShouldShowSource(value.GetCurrentSource());
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected abstract void OnRoomChange(RoomBase room);

        private void RoomOnSourceChangedInternal(RoomBase room, SourceChangedEventArgs args)
        {
            RoomOnSourceChanged(room, args);
            if (args.Source != null && args.RoomSourceIndex == 1)
            {
                UIShouldShowSource(args.Source);
            }
        }

        protected abstract void UIShouldShowSource(SourceBase source);

        protected abstract void RoomOnSourceChanged(RoomBase room, SourceChangedEventArgs args);

        public abstract void RoomPoweringOff();

        protected void OnLightSensorValueChanged(ushort lightSensorValue)
        {
        }

        protected virtual void ShowDefaultPage()
        {
            try
            {
                Pages.FirstOrDefault()?.Show();
            }
            catch (Exception e)
            {
                Logger.Error($"{ToString()}: Could not show default page, {e.Message}");
            }
        }

        public void BrowserOpen()
        {
            ExtenderApplication.InvokeMethod("OpenBrowser");
        }

        public void BrowserClose()
        {
            ExtenderApplication.InvokeMethod("CloseBrowser");
        }

        public void BrowserKioskOn()
        {
            ExtenderApplication.InvokeMethod("BrowserKioskOn");
        }

        public void BrowserKioskOff()
        {
            ExtenderApplication.InvokeMethod("BrowserKioskOff");
        }

        protected abstract void OnHardButtonEvent(IButton button, Components.ButtonEventArgs args);

        internal void InitializeInternal()
        {
            _defaultRoom = UxEnvironment.GetRoom(_roomId);
            OnInitialize(_defaultRoom);
            Room = _defaultRoom;
            Task.Run(ShowDefaultPage);
        }

        protected abstract void OnInitialize(RoomBase defaultRoom);

        public override string ToString()
        {
            return $"{Device}";
        }
    }

    public delegate void RoomChangeEventHandler(Core3ControllerBase controller, RoomBase newRoom);
}