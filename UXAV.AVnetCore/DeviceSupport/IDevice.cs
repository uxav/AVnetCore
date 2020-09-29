using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Diagnostics;
using UXAV.AVnetCore.Models.Rooms;

namespace UXAV.AVnetCore.DeviceSupport
{
    public interface IDevice : IDiagnosticItem, IInitializable
    {
        string ConnectionInfo { get; }

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

        /// <summary>
        /// Version information string
        /// </summary>
        string VersionInfo { get; }

        string Identity { get; }

        RoomBase AllocatedRoom { get; }

        bool DeviceCommunicating { get; }
        
        bool DebugEnabled { get; set; }

        bool DebugEnabled { get; set; }

        /// <summary>
        /// Event called if the comms status changes on the device
        /// </summary>
        event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;
    }

    public delegate void DeviceCommunicatingChangeHandler(IDevice device, bool communicating);
}