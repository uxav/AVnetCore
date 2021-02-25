using UXAV.AVnetCore.Models;

namespace UXAV.AVnetCore.DeviceSupport
{
    public interface IConnectedItem : IGenericItem
    {
        string ConnectionInfo { get; }
        bool DeviceCommunicating { get; }

        /// <summary>
        /// Event called if the comms status changes on the device
        /// </summary>
        event DeviceCommunicatingChangeHandler DeviceCommunicatingChange;
    }

    public delegate void DeviceCommunicatingChangeHandler(IConnectedItem device, bool communicating);
}