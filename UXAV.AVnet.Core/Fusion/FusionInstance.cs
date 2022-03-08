using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.Fusion;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.AVnet.Core.Models;
using UXAV.AVnet.Core.Models.Rooms;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Fusion
{
    public class FusionInstance
    {
        private readonly Dictionary<uint, IFusionAsset> _fusionAssets = new Dictionary<uint, IFusionAsset>();

        internal FusionInstance(FusionRoom fusionRoom, RoomBase room)
        {
            FusionRoom = fusionRoom;
            Room = room;
            FusionRoom.OnlineStatusChange += FusionRoomOnOnlineStatusChange;
            FusionRoom.FusionStateChange += FusionRoomOnFusionStateChange;
            FusionRoom.FusionAssetStateChange += FusionRoomOnFusionAssetStateChange;
        }

        public RoomBase Room { get; }

        public FusionRoom FusionRoom { get; }

        private uint GetNextAvailableAssetKey()
        {
            for (var key = 1U; key <= 249; key++)
                if (!_fusionAssets.ContainsKey(key))
                    return key;

            throw new IndexOutOfRangeException("No more asset keys available");
        }

        private uint GetKeyForAssetDevice(IFusionAsset device)
        {
            foreach (var fusionAsset in _fusionAssets)
                if (fusionAsset.Value == device)
                    return fusionAsset.Key;

            throw new KeyNotFoundException("No device exists in collection");
        }

        public void AddAsset(IFusionAsset asset)
        {
            var key = GetNextAvailableAssetKey();
            AddAsset(asset, key);
        }

        public void AddAsset(IFusionAsset asset, uint key)
        {
            if (_fusionAssets.ContainsKey(key))
                throw new ArgumentException($"Asset with key {key} already exists", nameof(key));

            FusionRoom.AddAsset(eAssetType.StaticAsset, key,
                asset.Name, asset.FusionAssetType.ToString(), Guid.NewGuid().ToString());

            _fusionAssets.Add(key, asset);

            var fusionAsset = (FusionStaticAsset)FusionRoom.UserConfigurableAssetDetails[key].Asset;
            fusionAsset.ParamMake.Value = asset.ManufacturerName;
            fusionAsset.ParamModel.Value = asset.ModelName;

            fusionAsset.AddSig(eSigType.String, 1, "Identity", eSigIoMask.InputSigOnly);
            fusionAsset.AddSig(eSigType.String, 2, "Serial Number", eSigIoMask.InputSigOnly);
            fusionAsset.AddSig(eSigType.String, 3, "Connection Info", eSigIoMask.InputSigOnly);
            fusionAsset.AddSig(eSigType.String, 4, "Version Info", eSigIoMask.InputSigOnly);

            if (asset is IConnectedItem connectedItem)
            {
                fusionAsset.Connected.InputSig.BoolValue = connectedItem.DeviceCommunicating;
                connectedItem.DeviceCommunicatingChange += AssetOnDeviceCommunicatingChange;
            }

            if (asset is IPowerDevice powerDevice)
            {
                fusionAsset.PowerOn.InputSig.BoolValue = powerDevice.Power;
                powerDevice.PowerStatusChange += PowerDeviceOnPowerStatusChange;
            }
        }

        private void PowerDeviceOnPowerStatusChange(IPowerDevice device, DevicePowerStatusEventArgs args)
        {
            var key = GetKeyForAssetDevice((IFusionAsset)device);
            var fusionAsset = (FusionStaticAsset)FusionRoom.UserConfigurableAssetDetails[key].Asset;
            fusionAsset.PowerOn.InputSig.BoolValue = device.Power;
            if (device is DisplayDeviceBase)
                Task.Run(() =>
                {
                    var displayDevices =
                        UxEnvironment.System.DevicesDict.Values.Where(d =>
                            d is DisplayDeviceBase && d.AllocatedRoom == Room).Cast<DisplayDeviceBase>();
                    var powerFeedback = displayDevices.Any(d => d.Power);
                    FusionRoom.DisplayPowerOn.InputSig.BoolValue = powerFeedback;
                });
        }

        private void AssetOnDeviceCommunicatingChange(IConnectedItem device, bool communicating)
        {
            var key = GetKeyForAssetDevice((IFusionAsset)device);
            var fusionAsset = (FusionStaticAsset)FusionRoom.UserConfigurableAssetDetails[key].Asset;
            fusionAsset.Connected.InputSig.BoolValue = device.DeviceCommunicating;
        }

        private void FusionRoomOnOnlineStatusChange(GenericBase currentdevice, OnlineOfflineEventArgs args)
        {
            if (!args.DeviceOnLine) return;

            Task.Run(() =>
            {
                foreach (var kvp in _fusionAssets)
                {
                    var staticAsset = FusionRoom.UserConfigurableAssetDetails[kvp.Key].Asset as FusionStaticAsset;

                    if (staticAsset == null) continue;

                    var asset = kvp.Value;

                    staticAsset.FusionGenericAssetSerialsAsset3.StringInput[50].StringValue = asset.Identity;
                    staticAsset.FusionGenericAssetSerialsAsset3.StringInput[51].StringValue = asset.SerialNumber;

                    if (asset is IConnectedItem connectedItem)
                    {
                        staticAsset.FusionGenericAssetSerialsAsset3.StringInput[52].StringValue =
                            connectedItem.ConnectionInfo;
                        staticAsset.Connected.InputSig.BoolValue = connectedItem.DeviceCommunicating;
                    }

                    if (asset is IDevice device)
                        staticAsset.FusionGenericAssetSerialsAsset3.StringInput[53].StringValue = device.VersionInfo;

                    if (asset is IPowerDevice powerDevice) staticAsset.PowerOn.InputSig.BoolValue = powerDevice.Power;
                }

                FusionRoom.SystemPowerOn.InputSig.BoolValue = Room.Power;
                var displayDevices =
                    UxEnvironment.System.DevicesDict.Values.Where(d =>
                        d is DisplayDeviceBase && d.AllocatedRoom == Room).Cast<DisplayDeviceBase>();
                var powerFeedback = displayDevices.Any(d => d.Power);
                FusionRoom.DisplayPowerOn.InputSig.BoolValue = powerFeedback;
            });
        }

        private void FusionRoomOnFusionStateChange(FusionBase device, FusionStateEventArgs args)
        {
            switch (args.EventId)
            {
                case FusionEventIds.SystemPowerOffReceivedEventId:
                    if (FusionRoom.SystemPowerOff.OutputSig.BoolValue)
                    {
                        Logger.Highlight($"Fusion requested power off in {Room.Name}");
                        Task.Run(() => Room.FusionRequestedPowerOff());
                    }

                    break;
                case FusionEventIds.SystemPowerOnReceivedEventId:
                    if (FusionRoom.SystemPowerOn.OutputSig.BoolValue)
                    {
                        Logger.Highlight($"Fusion requested power on in {Room.Name}");
                        Task.Run(() => Room.FusionRequestedPowerOn());
                    }

                    break;
                case FusionEventIds.DisplayPowerOffReceivedEventId:
                    if (FusionRoom.DisplayPowerOff.OutputSig.BoolValue)
                    {
                        var displays = UxEnvironment.System.DevicesDict.Values
                            .Where(d => d is DisplayDeviceBase)
                            .Cast<DisplayDeviceBase>()
                            .Where(d => d.AllocatedRoom == Room);

                        Logger.Highlight($"Fusion requested displays off in {Room.Name}");
                        foreach (var display in displays) display.Power = false;
                    }

                    break;
                case FusionEventIds.DisplayPowerOnReceivedEventId:
                    if (FusionRoom.DisplayPowerOn.OutputSig.BoolValue)
                    {
                        var displays = UxEnvironment.System.DevicesDict.Values
                            .Where(d => d is DisplayDeviceBase)
                            .Cast<DisplayDeviceBase>()
                            .Where(d => d.AllocatedRoom == Room);

                        Logger.Highlight($"Fusion requested displays on in {Room.Name}");
                        foreach (var display in displays) display.Power = true;
                    }

                    break;
            }
        }

        private void FusionRoomOnFusionAssetStateChange(FusionBase device, FusionAssetStateEventArgs args)
        {
            var asset =
                FusionRoom.UserConfigurableAssetDetails[args.UserConfigurableAssetDetailIndex].Asset as
                    FusionStaticAsset;

            if (asset == null) return;
            // ReSharper disable once SuspiciousTypeConversion.Global
            var powerDevice = _fusionAssets[args.UserConfigurableAssetDetailIndex] as IPowerDevice;

            switch (args.EventId)
            {
                case FusionAssetEventId.StaticAssetPowerOnReceivedEventId:
                    if (asset.PowerOn.OutputSig.BoolValue)
                        if (powerDevice != null)
                            powerDevice.Power = true;

                    break;
                case FusionAssetEventId.StaticAssetPowerOffReceivedEventId:
                    if (asset.PowerOff.OutputSig.BoolValue)
                        if (powerDevice != null)
                            powerDevice.Power = false;

                    break;
            }
        }
    }
}