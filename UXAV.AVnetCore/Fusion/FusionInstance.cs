﻿using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.Fusion;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Rooms;

namespace UXAV.AVnetCore.Fusion
{
    public class FusionInstance
    {
        private readonly FusionRoom _fusionRoom;
        private readonly RoomBase _room;
        private readonly Dictionary<uint, IFusionAsset> _fusionAssets = new Dictionary<uint, IFusionAsset>();

        internal FusionInstance(FusionRoom fusionRoom, RoomBase room)
        {
            _fusionRoom = fusionRoom;
            _room = room;
            _fusionRoom.OnlineStatusChange += FusionRoomOnOnlineStatusChange;
            _fusionRoom.FusionStateChange += FusionRoomOnFusionStateChange;
            _fusionRoom.FusionAssetStateChange += FusionRoomOnFusionAssetStateChange;
        }

        public RoomBase Room => _room;

        public FusionRoom FusionRoom => _fusionRoom;

        private uint GetNextAvailableAssetKey()
        {
            for (var key = 1U; key <= 249; key++)
            {
                if (!_fusionAssets.ContainsKey(key))
                {
                    return key;
                }
            }

            throw new IndexOutOfRangeException("No more asset keys available");
        }

        private uint GetKeyForAssetDevice(IDevice device)
        {
            foreach (var fusionAsset in _fusionAssets)
            {
                if (fusionAsset.Value == device)
                {
                    return fusionAsset.Key;
                }
            }

            throw new KeyNotFoundException("No device exists in collection");
        }

        internal void AddAsset(IFusionAsset asset)
        {
            var key = GetNextAvailableAssetKey();
            FusionRoom.AddAsset(eAssetType.StaticAsset, key,
                asset.Name, asset.FusionAssetType.ToString(), Guid.NewGuid().ToString());

            _fusionAssets.Add(key, asset);

            var fusionAsset = (FusionStaticAsset) FusionRoom.UserConfigurableAssetDetails[key].Asset;
            fusionAsset.ParamMake.Value = asset.ManufacturerName;
            fusionAsset.ParamModel.Value = asset.ModelName;
            fusionAsset.Connected.InputSig.BoolValue = asset.DeviceCommunicating;

            fusionAsset.AddSig(eSigType.String, 1, "Connection Info", eSigIoMask.InputSigOnly);
            fusionAsset.AddSig(eSigType.String, 2, "Serial Number", eSigIoMask.InputSigOnly);
            fusionAsset.AddSig(eSigType.String, 3, "Version Info", eSigIoMask.InputSigOnly);
            fusionAsset.AddSig(eSigType.String, 4, "Identity", eSigIoMask.InputSigOnly);

            asset.DeviceCommunicatingChange += AssetOnDeviceCommunicatingChange;

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (asset is IPowerDevice powerDevice)
            {
                fusionAsset.PowerOn.InputSig.BoolValue = powerDevice.Power;
                powerDevice.PowerStatusChange += PowerDeviceOnPowerStatusChange;
            }
        }

        private void PowerDeviceOnPowerStatusChange(IPowerDevice device, DevicePowerStatusEventArgs args)
        {
            var key = GetKeyForAssetDevice(device);
            var fusionAsset = (FusionStaticAsset) FusionRoom.UserConfigurableAssetDetails[key].Asset;
            fusionAsset.PowerOn.InputSig.BoolValue = device.Power;
        }

        private void AssetOnDeviceCommunicatingChange(IDevice device, bool communicating)
        {
            var key = GetKeyForAssetDevice(device);
            var fusionAsset = (FusionStaticAsset) FusionRoom.UserConfigurableAssetDetails[key].Asset;
            fusionAsset.Connected.InputSig.BoolValue = device.DeviceCommunicating;
        }

        private void FusionRoomOnOnlineStatusChange(GenericBase currentdevice, OnlineOfflineEventArgs args)
        {
            foreach (var asset in _fusionAssets)
            {
                var staticAsset = _fusionRoom.UserConfigurableAssetDetails[asset.Key].Asset as FusionStaticAsset;

                if (staticAsset == null) continue;

                var device = asset.Value;

                staticAsset.FusionGenericAssetSerialsAsset3.StringInput[50].StringValue = device.ConnectionInfo;
                staticAsset.FusionGenericAssetSerialsAsset3.StringInput[51].StringValue = device.SerialNumber;
                staticAsset.FusionGenericAssetSerialsAsset3.StringInput[52].StringValue = device.VersionInfo;
                staticAsset.FusionGenericAssetSerialsAsset3.StringInput[53].StringValue = device.Identity;
            }
        }

        private void FusionRoomOnFusionStateChange(FusionBase device, FusionStateEventArgs args)
        {
            switch (args.EventId)
            {
                case FusionEventIds.SystemPowerOffReceivedEventId:
                    if (_fusionRoom.SystemPowerOff.OutputSig.BoolValue)
                    {
                        _room.PowerOff();
                    }

                    break;
                case FusionEventIds.SystemPowerOnReceivedEventId:
                    if (_fusionRoom.SystemPowerOn.OutputSig.BoolValue)
                    {
                        _room.PowerOn();
                    }

                    break;
                case FusionEventIds.DisplayPowerOffReceivedEventId:
                    if (_fusionRoom.DisplayPowerOff.OutputSig.BoolValue)
                    {
                        var displays = UxEnvironment.System.DevicesDict.Values
                            .Where(d => d is DisplayDeviceBase)
                            .Cast<DisplayDeviceBase>()
                            .Where(d => d.AllocatedRoom == _room);
                        foreach (var display in displays)
                        {
                            display.Power = false;
                        }
                    }

                    break;
                case FusionEventIds.DisplayPowerOnReceivedEventId:
                    if (_fusionRoom.DisplayPowerOn.OutputSig.BoolValue)
                    {
                        var displays = UxEnvironment.System.DevicesDict.Values
                            .Where(d => d is DisplayDeviceBase)
                            .Cast<DisplayDeviceBase>()
                            .Where(d => d.AllocatedRoom == _room);
                        foreach (var display in displays)
                        {
                            display.Power = true;
                        }
                    }

                    break;
            }
        }

        private void FusionRoomOnFusionAssetStateChange(FusionBase device, FusionAssetStateEventArgs args)
        {
            var asset =
                _fusionRoom.UserConfigurableAssetDetails[args.UserConfigurableAssetDetailIndex].Asset as
                    FusionStaticAsset;

            if (asset == null) return;
            // ReSharper disable once SuspiciousTypeConversion.Global
            var powerDevice = _fusionAssets[args.UserConfigurableAssetDetailIndex] as IPowerDevice;

            switch (args.EventId)
            {
                case FusionAssetEventId.StaticAssetPowerOnReceivedEventId:
                    if (asset.PowerOn.OutputSig.BoolValue)
                    {
                        if (powerDevice != null)
                        {
                            powerDevice.Power = true;
                        }
                    }

                    break;
                case FusionAssetEventId.StaticAssetPowerOffReceivedEventId:
                    if (asset.PowerOff.OutputSig.BoolValue)
                    {
                        if (powerDevice != null)
                        {
                            powerDevice.Power = false;
                        }
                    }

                    break;
            }
        }
    }
}