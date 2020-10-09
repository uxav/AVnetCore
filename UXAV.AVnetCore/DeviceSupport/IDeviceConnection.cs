namespace UXAV.AVnetCore.DeviceSupport
{
    public interface IDeviceConnection
    {
        void Connect();
        void Disconnect();
        bool Connected { get; }
        string Address { get; }
        bool DebugEnabled { get; set; }
        event ConnectedStatusChangeEventHandler ConnectedChange;
        event ReceivedDataEventHandler ReceivedData;
        void Send(byte[] bytes, int index, int count);

        DeviceConnectionType ConnectionType { get; }
    }

    public enum DeviceConnectionType
    {
        TcpSocket,
        UdpSocket,
        Serial,
    }

    public delegate void ConnectedStatusChangeEventHandler(IDeviceConnection client, bool connected);

    public delegate void ReceivedDataEventHandler(IDeviceConnection client, byte[] bytes);
}