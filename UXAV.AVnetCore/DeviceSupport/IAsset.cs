using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Rooms;

namespace UXAV.AVnetCore.DeviceSupport
{
    public interface IAsset : IGenericItem
    {
        /// <summary>
        /// The name of the manufacturer for the device
        /// </summary>
        string ManufacturerName { get; }

        /// <summary>
        /// The model name of the device
        /// </summary>
        string ModelName { get; }

        /// <summary>
        /// Return device serial number
        /// </summary>
        string SerialNumber { get; }

        string Identity { get; }

        RoomBase AllocatedRoom { get; }
    }
}