using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpProInternal;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;
using IPAddress = System.Net.IPAddress;
using IPEndPoint = System.Net.IPEndPoint;

namespace UXAV.AVnet.Core.DeviceSupport
{
    public class FireMonitor : IInitializable
    {
        private readonly PortDevice _port;
        private readonly EventWaitHandle _sendWait = new EventWaitHandle(true, EventResetMode.AutoReset);
        private UdpClient _client;
        private bool _fireState;
        private bool _initialized;
        private bool _normalState;
        private bool _programStopping;
        private int _sendCount;
        private TimeSpan _sendWaitTime = TimeSpan.FromSeconds(30);
        private int _udpPort;

        public FireMonitor(Versiport versiPort)
        {
            _port = versiPort;
            if (!_port.Registered) _port.Register();

            versiPort.SetVersiportConfiguration(eVersiportConfiguration.DigitalInput);
            versiPort.VersiportChange += PortOnVersiportChange;

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping) _programStopping = true;
            };
        }

        public FireMonitor(DigitalInput digitalInput)
        {
            _port = digitalInput;
            if (!_port.Registered) _port.Register();

            digitalInput.StateChange += DigitalInputOnStateChange;

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping) _programStopping = true;
            };
        }

        public FireMonitor(int udpListenPort)
        {
            _client = new UdpClient(udpListenPort);
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping) _programStopping = true;
            };
            Task.Run(() =>
            {
                while (!_programStopping)
                {
                    var endpoint = new IPEndPoint(IPAddress.Any, _udpPort);
                    var bytes = _client.Receive(ref endpoint);
                    if (bytes[0] == 0x02 && bytes[4] == 0x03)
                        if (Encoding.ASCII.GetString(bytes, 1, 2) == "FM")
                            FireState = Convert.ToBoolean(bytes[3]);
                }
            });
        }

        public bool FireState
        {
            get => _fireState;
            set
            {
                if (value == _fireState) return;
                _fireState = value;
                Logger.Warn($"Fire state changed to: {_fireState}");
                OnStateChange(_fireState);
            }
        }

        public uint Id => 0;
        public string Name => "Fire Interface";

        public void Initialize()
        {
            switch (_port)
            {
                case Versiport versiport:
                    _normalState = versiport.DigitalIn;
                    break;
                case DigitalInput digitalInput:
                    _normalState = digitalInput.State;
                    break;
            }

            Logger.Highlight($"Fire interface state set as {_port}, normal state = " +
                             (_normalState ? "closed" : "open"));
            _initialized = true;
        }

        public event FireStateChangeHandler FireStateChanged;

        private void PortOnVersiportChange(Versiport port, VersiportEventArgs args)
        {
            if (args.Event != eVersiportEvent.DigitalInChange) return;
            if (!_initialized)
            {
                Logger.Log($"Fire versiport = {port.DigitalIn}" +
                           ", not yet initialized so ignoring and will use this as normal value when it does");
                return;
            }

            Logger.Warn($"Fire versiport = {port.DigitalIn}");
            FireState = port.DigitalIn != _normalState;
        }

        private void DigitalInputOnStateChange(DigitalInput digitalinput, DigitalInputEventArgs args)
        {
            if (!_initialized)
            {
                Logger.Log($"Fire digital input = {args.State}" +
                           ", not yet initialized so ignoring and will use this as normal value when it does");
                return;
            }

            Logger.Warn($"Fire digital input = {args.State}");
            FireState = args.State != _normalState;
        }

        protected virtual void OnStateChange(bool fireStateActive)
        {
            FireStateChanged?.Invoke(this, fireStateActive);
            if (_client == null || _port == null) return; // don't need to send
            _sendCount = 0;
            _sendWaitTime = TimeSpan.FromSeconds(1);
            _sendWait.Set();
        }

        public void SetupUdpSocket(int port)
        {
            if (_client != null) throw new InvalidOperationException("Listen client already started");

            _udpPort = port;
            _client = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            Task.Run(() =>
            {
                while (!_programStopping)
                    try
                    {
                        if (_initialized)
                        {
                            _sendCount++;
                            if (_sendCount >= 5)
                            {
                                _sendWaitTime = TimeSpan.FromSeconds(30);
                                _sendCount = 0;
                            }

                            var bytes = new byte[]
                            {
                                0x02, 0x46, 0x4D, Convert.ToByte(_fireState), 0x03
                            };
                            _client.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, _udpPort));
                        }

                        _sendWait.WaitOne(_sendWaitTime);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                Logger.Warn("Leaving fire udp broadcast loop");
            });
        }
    }

    public delegate void FireStateChangeHandler(FireMonitor monitor, bool fireState);
}