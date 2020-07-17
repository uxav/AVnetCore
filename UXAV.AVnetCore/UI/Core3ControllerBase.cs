using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Rooms;
using UXAV.AVnetCore.UI.Components;
using UXAV.AVnetCore.UI.Components.Views;
using UXAV.AVnetCore.UI.ReservedJoins;
using UXAV.Logging;

namespace UXAV.AVnetCore.UI
{
    public abstract class Core3ControllerBase : ISigProvider
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
            ExtenderApplication = UseDeviceExtenderByName("ExtenderApplicationControlReservedSigs");
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
            ExtenderUsbLedAccessory = UseDeviceExtenderByName("ExtenderUsbLedAccessoryControlReservedSigs");
            ExtenderIntegratedLightBar = UseDeviceExtenderByName("ExtenderIntegratedLightBarReservedSigs");
            ExtenderScreenSaver = UseDeviceExtenderByName("ExtenderScreenSaverReservedSigs");
            ExtenderTouchDetection = UseDeviceExtenderByName("ExtenderTouchDetectionReservedSigs");

            Pages = new UIPageCollection(this);

            HardButtons = new UIButtonCollection();
            for (var button = 1U; button <= 5; button++)
            {
                HardButtons.Add(new UIHardButton(this, button, ExtenderHardButton));
            }

            HardButtons.ButtonEvent += OnHardButtonEvent;

            var files = Directory.GetFiles(Crestron.SimplSharp.CrestronIO.Directory.GetApplicationDirectory(),
                $"*.sgd");
            // Look for SGD files with priority given to file names containing the device type name... ie 'CrestronApp'
            foreach (var file in files.OrderByDescending(f => f.Contains(Device.GetType().Name)))
            {
                Logger.Log("Found SGD File: {0}, Loading...", file);
                try
                {
                    using (var fileStream = File.OpenRead(file))
                    {
                        Device.LoadSmartObjects(fileStream.GetCrestronStream());
                    }

                    break;
                }
                catch (Exception e)
                {
                    Logger.Error("Error loading SGD file, {0}", e.Message);
                }
            }
        }

        private DeviceExtender UseDeviceExtenderByName(string name)
        {
            try
            {
                var extender = (DeviceExtender) Device.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => p.Name == name)
                    ?.GetValue(Device, null);
                if (extender != null)
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

        protected DeviceExtender ExtenderUsbLedAccessory { get; }

        protected DeviceExtender ExtenderIntegratedLightBar { get; }

        protected DeviceExtender ExtenderScreenSaver { get; }

        protected DeviceExtender ExtenderTouchDetection { get; }

        protected DeviceExtender ExtenderAutoUpdate { get; }

        private void OnIpInformationChange(GenericBase currentdevice, ConnectedIpEventArgs args)
        {
            if (args.Connected)
            {
                Logger.Log($"{currentdevice} has connected with IP: " + args.DeviceIpAddress);
                IpAddress = args.DeviceIpAddress;
                if (ExtenderSystem != null)
                {
                    Logger.Log($"{Device} Turning Backlight On");
                    ExtenderSystem.InvokeMethod("BacklightOn");
                }
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

        private void OnDeviceExtenderSigChange(DeviceExtender extender, SigEventArgs args)
        {
            var extenderName = _deviceExtenderNames.ContainsKey(extender)
                ? _deviceExtenderNames[extender]
                : extender.GetType().Name;
            var sigName = extender.GetSigPropertyName(args.Sig);
            if (string.IsNullOrEmpty(sigName)) return;
            Logger.Log(Logger.LoggerLevel.Debug,
                $"{Device} {extenderName}.{sigName} = {args.Sig}");

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
            }
        }

        public RoomBase DefaultRoom => _defaultRoom;

        public RoomBase Room
        {
            get => _room;
            set
            {
                if (_room == value) return;
                _room = value;
                OnRoomChangeInternal(value);
            }
        }

        private void OnRoomChangeInternal(RoomBase value)
        {
            Logger.Highlight($"{this} Room Set: {value}");
            try
            {
                OnRoomChange(value);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected abstract void OnRoomChange(RoomBase value);

        protected virtual void ShowDefaultPage()
        {
            Pages.FirstOrDefault()?.Show();
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
}