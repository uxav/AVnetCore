namespace UXAV.AVnet.Core.DeviceSupport
{
    public interface IDeviceConnection
    {
        bool Connected { get; }
        string Address { get; }
        bool DebugEnabled { get; set; }

        DeviceConnectionType ConnectionType { get; }
        void Connect();
        void Disconnect();
        event ConnectedStatusChangeEventHandler ConnectedChange;
        event ReceivedDataEventHandler ReceivedData;
        void Send(byte[] bytes, int index, int count);
    }

    public enum DeviceConnectionType
    {
        TcpSocket,
        UdpSocket,
        Serial
    }

    public delegate void ConnectedStatusChangeEventHandler(IDeviceConnection client, bool connected);

    public delegate void ReceivedDataEventHandler(IDeviceConnection client, byte[] bytes);
}