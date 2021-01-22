using System;
using System.Text;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpProInternal;
using UXAV.Logging;

namespace UXAV.AVnetCore.DeviceSupport
{
    public class ComPortHandler : IDeviceConnection
    {
        private readonly IComPortDevice _portDevice;
        private readonly ComPort.ComPortSpec _portSpec;
        private bool _init;
        private bool _connected;

        public ComPortHandler(IComPortDevice portDevice, ComPort.ComPortSpec portSpec)
        {
            _portDevice = portDevice;
            _portSpec = portSpec;
        }

        public void Register()
        {
            Logger.Log($"Attempting to register port device: {_portDevice}");
            if (!(_portDevice is CrestronDevice port) || port.Registered)
            {
                Logger.Log("Port does not need to register");
                return;
            }
            if (port.ParentDevice is CresnetDevice parent)
            {
                if (!parent.Registered)
                {
                    Logger.Log("Skipping device registration as parent is not registered yet");
                    return;
                }
            }
            var result = port.Register();
            if (result == eDeviceRegistrationUnRegistrationResponse.Success)
            {
                Logger.Success($"Registered port device: {_portDevice} ok!");
                return;
            }

            Logger.Error("Could not register comport {0}, {1}", _portDevice.ToString(), result);
        }

        public void Connect()
        {
            Register();
            if (_init) return;
            _init = true;
            _portDevice.SetComPortSpec(_portSpec);
            _portDevice.SerialDataReceived += (device, args) =>
            {
                var data = Encoding.ASCII.GetBytes(args.SerialData);
                OnReceivedData(this, data);
            };
            Connected = true;
        }

        public void Disconnect()
        {
            Connected = false;
        }

        public bool Connected
        {
            get => _connected;
            private set
            {
                if (_connected == value) return;
                _connected = value;
                OnConnectedChange(this, _connected);
            }
        }

        public string Address => _portDevice.ToString();
        public bool DebugEnabled { get; set; }
        public event ConnectedStatusChangeEventHandler ConnectedChange;
        public event ReceivedDataEventHandler ReceivedData;

        public void Send(byte[] bytes, int index, int count)
        {
            _portDevice.Send(bytes, index, count);
        }

        public DeviceConnectionType ConnectionType => DeviceConnectionType.Serial;

        protected virtual void OnReceivedData(IDeviceConnection client, byte[] bytes)
        {
            try
            {
                Connected = true;
                ReceivedData?.Invoke(client, bytes);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected virtual void OnConnectedChange(IDeviceConnection client, bool connected)
        {
            try
            {
                ConnectedChange?.Invoke(client, connected);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}