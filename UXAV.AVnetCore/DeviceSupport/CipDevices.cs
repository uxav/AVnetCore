using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Diagnostics;
using UXAV.Logging;

namespace UXAV.AVnetCore.DeviceSupport
{
    public static class CipDevices
    {
        private static readonly ConcurrentDictionary<uint, GenericDevice> Devices =
            new ConcurrentDictionary<uint, GenericDevice>();

        public static void Init(CrestronControlSystem controlSystem)
        {
            ControlSystem = controlSystem;
        }

        public static CrestronControlSystem ControlSystem { get; private set; }

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
                    var assembly = Assembly.LoadFile(file.FullName);
                    var type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        Logger.Debug($"Found type: {type.Name}");
                        return type;
                    }
                }

                if (search == "*") break;
                if (!search.EndsWith("*"))
                {
                    search = search + "*";
                    continue;
                }
                Logger.Debug($"Could not find using search: {search}.dll");
                if (search != "Crestron*")
                {
                    search = "Crestron*";
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

        public static IEnumerable<DiagnosticMessage> GetDiagnosticMessages()
        {
            return Devices.Values.Select(device => device.CreateStatusMessage());
        }

        internal static void RegisterDevices()
        {
            foreach (var device in Devices.Values.Where(d => !d.Registered))
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