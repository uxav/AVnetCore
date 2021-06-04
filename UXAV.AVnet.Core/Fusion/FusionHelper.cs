using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.AudioDistribution;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.AVnet.Core.Models.Rooms;

namespace UXAV.AVnet.Core.Fusion
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

        public static FusionInstance GetFusionInstance(this RoomBase room)
        {
            return !Rooms.ContainsKey(room.Id) ? null : Rooms[room.Id];
        }

        public static void CreateFusionAsset(IFusionAsset device)
        {
            if (device.AllocatedRoom == null)
            {
                throw new ArgumentException("Device does not have allocated room", nameof(device));
            }

            var fusionInstance = device.AllocatedRoom.GetFusionInstance();
            fusionInstance.AddAsset(device);
        }
    }
}