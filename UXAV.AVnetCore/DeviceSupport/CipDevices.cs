using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.Fusion;
using Crestron.SimplSharpPro.UI;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Diagnostics;
using UXAV.Logging;

namespace UXAV.AVnetCore.DeviceSupport
{
    public static class CipDevices
    {
        private static readonly ConcurrentDictionary<uint, GenericDevice> Devices =
            new ConcurrentDictionary<uint, GenericDevice>();

        private static readonly ConcurrentDictionary<uint, string> XPanelFilePaths =
            new ConcurrentDictionary<uint, string>();

        public static void Init(CrestronControlSystem controlSystem)
        {
            ControlSystem = controlSystem;
        }

        public static CrestronControlSystem ControlSystem { get; private set; }

        public static uint GetNextAvailableIpId()
        {
            return GetNextAvailableIpId(0x03);
        }

        public static uint GetNextAvailableIpId(uint ipId)
        {
            if(ipId < 0x03) throw new IndexOutOfRangeException("id must be greater than 0x03");
            for (var id = ipId; id <= 0xFE; id++)
            {
                if(ContainsDevice(id)) continue;
                return id;
            }
            throw new InvalidOperationException("No more ID's available");
        }

        public static IEnumerable<uint> GetUsedIpIds()
        {
            return Devices.Keys.ToArray();
        }

        public static GenericDevice GetOrCreateDevice(string typeName, uint ipId, string description)
        {
            if (Devices.ContainsKey(ipId))
            {
                return GetDevice(ipId);
            }

            return CreateDevice(typeName, ipId, description);
        }

        public static GenericDevice GetOrCreateDevice(string typeName, uint ipId, string ipAddressOrHostname,
            string description)
        {
            if (Devices.ContainsKey(ipId))
            {
                return GetDevice(ipId);
            }

            return CreateDevice(typeName, ipId, ipAddressOrHostname, description);
        }

        public static GenericDevice CreateDevice(string typeName, uint ipId, string description)
        {
            if (Devices.ContainsKey(ipId))
            {
                throw new ArgumentException($"Device with ID {ipId:X2} already exists", nameof(ipId));
            }

            var type = GetType(typeName);
            var ctor = type.GetConstructor(new[] {typeof(uint), typeof(CrestronControlSystem)});
            if (ctor == null)
            {
                throw new Exception(
                    "Could not find ctor in the form of (uint, CrestronControlSystem)");
            }

            var device = (GenericDevice) ctor.Invoke(new object[] {ipId, ControlSystem});
            device.Description = description;
            Devices[device.ID] = device;
            device.OnlineStatusChange += DeviceOnOnlineStatusChange;
            return device;
        }

        public static GenericDevice CreateXPanelForSmartGraphics(uint ipId, string description, string pathOfVtzFile)
        {
            var device = CreateDevice(typeof(XpanelForSmartGraphics).FullName, ipId, description);
            XPanelFilePaths[ipId] = pathOfVtzFile;
            return device;
        }

        public static GenericDevice CreateDevice(string typeName, uint ipId, string ipAddressOrHostname,
            string description)
        {
            if (Devices.ContainsKey(ipId))
            {
                throw new ArgumentException($"Device with ID {ipId:X2} already exists", nameof(ipId));
            }

            var type = GetType(typeName);
            var ctor = type.GetConstructor(new[]
            {
                typeof(uint),
                typeof(string),
                typeof(CrestronControlSystem)
            });
            if (ctor == null)
            {
                throw new Exception(
                    "Could not find ctor in the form of (uint, string, CrestronControlSystem)");
            }

            var device = (GenericDevice) ctor.Invoke(new object[] {ipId, ipAddressOrHostname, ControlSystem});
            device.Description = description;
            Devices[device.ID] = device;
            return device;
        }

        internal static FusionRoom CreateFusionRoom(uint ipId, string roomName, string description)
        {
            if (Devices.ContainsKey(ipId))
            {
                throw new ArgumentException($"Device with ID {ipId:X2} already exists", nameof(ipId));
            }

            var room = new FusionRoom(ipId, ControlSystem, roomName, Guid.NewGuid().ToString())
            {
                Description = description
            };
            Devices[room.ID] = room;
            return room;
        }

        private static void DeviceOnOnlineStatusChange(GenericBase currentdevice, OnlineOfflineEventArgs args)
        {
            EventService.Notify(EventMessageType.DeviceConnectionChange, new
            {
                @Device = currentdevice.Name,
                @Description = currentdevice.Description,
                @ConnectionInfo = $"IP ID: {currentdevice.ID:X2}",
                @Online = args.DeviceOnLine
            });
        }

        internal static string GetPathOfVtzFileForXPanel(uint ipId)
        {
            return XPanelFilePaths.ContainsKey(ipId) ? XPanelFilePaths[ipId] : string.Empty;
        }

        private static Type GetType(string typeName)
        {
            Logger.Debug($"Looking for assembly for {typeName}");
            var search = Regex.Match(typeName, @"^(?:([\w\.]+)\.)([\w\.]+)$").Groups[1].Value;
            var directory = new DirectoryInfo(SystemBase.ProgramApplicationDirectory);
            while (true)
            {
                Logger.Debug($"Looking at files matching pattern: {search}.dll");
                foreach (var file in directory.GetFiles($"{search}.dll"))
                {
                    Logger.Debug($"Will try load assembly file: {file.Name}");
                    try
                    {
                        var assembly = Assembly.LoadFile(file.FullName);
                        var type = assembly.GetType(typeName);
                        if (type == null) continue;
                        Logger.Debug($"Found type: {type.Name}");
                        return type;
                    }
                    catch (Exception e)
                    {
                        Logger.Warn($"Error trying to load assembly file: {file.Name}, {e.Message}");
                    }
                }

                if (search == "*") break;
                if (!search.EndsWith("*"))
                {
                    search = search + "*";
                    continue;
                }

                Logger.Debug($"Could not find using search: {search}.dll");
                if (search != "Crestron.*")
                {
                    search = "Crestron.*";
                    continue;
                }

                search = "*";
            }

            throw new Exception($"Could not load assembly for {typeName}");
        }

        public static bool ContainsDevice(uint ipId)
        {
            return Devices.ContainsKey(ipId);
        }

        public static GenericDevice GetDevice(uint ipId)
        {
            return Devices[ipId];
        }

        public static T GetDevice<T>(uint ipId) where T : GenericDevice
        {
            return Devices[ipId] as T;
        }

        public static IEnumerable<GenericDevice> GetDevices()
        {
            return Devices.Values;
        }

        public static IEnumerable<T> GetDevices<T>() where T : GenericDevice
        {
            return Devices.Values.Where(d => d is T).Cast<T>();
        }

        public static IEnumerable<DiagnosticMessage> GetDiagnosticMessages()
        {
            return Devices.Values.Select(device => device.CreateStatusMessage());
        }

        internal static void RegisterDevices()
        {
            foreach (var device in Devices.Values
                .Where(d => !(d is FusionRoom))
                .Where(d => !d.Registered))
            {
                try
                {
                    var result = device.Register();
                    if (result == eDeviceRegistrationUnRegistrationResponse.Success)
                    {
                        Logger.Success($"Registered device: {device}");
                        continue;
                    }

                    Logger.Error($"Could not register device: {device}, {result}");
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        internal static void RegisterFusionRooms()
        {
            Logger.Highlight("Registering Fusion Room instances now");
            foreach (var device in Devices.Values
                .Where(d => d is FusionRoom)
                .Where(d => !d.Registered))
            {
                try
                {
                    var result = device.Register();
                    if (result == eDeviceRegistrationUnRegistrationResponse.Success)
                    {
                        Logger.Success($"Registered device: {device}");
                        continue;
                    }

                    Logger.Error($"Could not register device: {device}, {result}");
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }
    }
}