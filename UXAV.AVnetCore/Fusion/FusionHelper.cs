using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.Fusion;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.AVnetCore.Models.Rooms;

namespace UXAV.AVnetCore.Fusion
{
    public static class FusionHelper
    {
        private static readonly Dictionary<uint, FusionInstance> Rooms = new Dictionary<uint, FusionInstance>();

        public static FusionInstance CreateFusionRoom(this RoomBase room, uint ipId)
        {
            var fusionRoom = CipDevices.CreateFusionRoom(ipId, room.Name, $"Fusion for room \"{room.Name}\"");
            var newInstance = new FusionInstance(fusionRoom, room);
            Rooms[room.Id] = newInstance;
            return newInstance;
        }

        public static FusionInstance GetFusionRoom(this RoomBase room)
        {
            return Rooms[room.Id];
        }

        public static void CreateFusionAsset(IFusionAsset device)
        {
            if (device.AllocatedRoom == null)
            {
                throw new ArgumentException("Device does not have allocated room", nameof(device));
            }

            var fusionInstance = device.AllocatedRoom.GetFusionRoom();
            fusionInstance.AddAsset(device);
        }
    }
}