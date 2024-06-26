﻿using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UXAV.Logging;

namespace UXAV.AVnet.Core.DeviceSupport
{
    public abstract class TcpConnectionBase : IDeviceConnection
    {
        private readonly int _port;
        private TcpClient _client;
        private Task _connectTask;
        private int _failConnectCount;

        private bool _remainConnected;
        private CancellationTokenSource _sendCancel;
        private Task _sendProcess;
        private BlockingCollection<byte[]> _sendQueue;
        private NetworkStream _stream;

        protected TcpConnectionBase(string address, int port)
        {
            Address = address;
            _port = port;
        }

        public bool Connected => _client != null && _client.Connected;
        public string Address { get; }
        public bool DebugEnabled { get; set; }

        public DeviceConnectionType ConnectionType => DeviceConnectionType.TcpSocket;

        public event ConnectedStatusChangeEventHandler ConnectedChange;
        public event ReceivedDataEventHandler ReceivedData;

        public void Connect()
        {
            _remainConnected = true;
            if (_client != null) throw new Exception("Already trying to connect or is connected");

            Logger.Debug($"{GetType().Name}.Connect() to Address: {Address}");
            _client = new TcpClient();

            _connectTask = Task.Run(ConnectionProcess);
        }

        public void Disconnect()
        {
            if (DebugEnabled) Logger.Debug($"{GetType().Name}.Disconnect()");

            _remainConnected = false;
            if (_client == null) return;
            try
            {
                if (_client.Connected) _client?.Dispose();

                if (_connectTask.Status == TaskStatus.Running) _connectTask.Dispose();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public virtual void Send(byte[] bytes, int index, int count)
        {
            SendEnqueue(bytes, index, count);
        }

        private void SendEnqueue(byte[] bytes, int index, int count)
        {
            if (_stream == null || !Connected)
            {
                Logger.Warn(1, "Cannot send, socket is disconnected!");
                return;
            }

            if (_sendQueue == null || _sendQueue.IsCompleted)
            {
                _sendCancel = new CancellationTokenSource();
                _sendQueue = new BlockingCollection<byte[]>();
            }

            var copiedBytes = new byte[count];
            Array.Copy(bytes, index, copiedBytes, 0, count);
            _sendQueue.Add(copiedBytes);

            if (_sendProcess != null && !_sendProcess.IsCompleted) return;
            try
            {
                _sendProcess = Task.Run(SendProcess);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void SendProcess()
        {
            if (DebugEnabled) Logger.Debug("Starting Send Process...");
            while (_stream != null && _stream.CanWrite)
                try
                {
                    var bytesToSend = _sendQueue.Take(_sendCancel.Token);
                    if (DebugEnabled)
                        Logger.Debug(
                            $"{GetType().Name} {Address} Tx: {Tools.GetBytesAsReadableString(bytesToSend, 0, bytesToSend.Length, true)}");

                    _stream.Write(bytesToSend, 0, bytesToSend.Length);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    Logger.Debug("Exiting send process as send queue is now marked as complete.");
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    break;
                }

            if (DebugEnabled) Logger.Debug("Exiting Send Process...");

            _sendProcess = null;
        }

        private async Task ConnectionProcess()
        {
            if (DebugEnabled) Logger.Debug($"{GetType().Name} Started {nameof(ConnectionProcess)}()");

            while (_client != null && _remainConnected)
            {
                try
                {
                    await _client.ConnectAsync(Address, _port);
                }
                catch (ObjectDisposedException)
                {
                    Logger.Warn($"{GetType().Name} {_client} disposed, exiting process");
                    _client = null;
                    if (_remainConnected) Connect();

                    return;
                }
                catch (Exception e)
                {
                    _failConnectCount++;
                    if (_failConnectCount == 5)
                        Logger.Error(
                            $"{GetType().Name} could not connect to {Address}, {e.GetType().Name}, {e.Message}");

                    Thread.Sleep(1000);
                    continue;
                }

                if (DebugEnabled) Logger.Debug($"{GetType().Name} Connected to {Address}, Getting stream..");

                _failConnectCount = 0;

                _stream = _client.GetStream();

                if (DebugEnabled) Logger.Debug($"{GetType().Name} Stream ok. Notifying online!");

                OnConnectedChange(this, true);

                var buffer = new byte[8192];
                while (true)
                    try
                    {
                        var count = 0;
                        try
                        {
                            count = _stream.Read(buffer, 0, buffer.Length);
                            if (DebugEnabled) Logger.Debug($"{GetType().Name} Stream read {count} bytes");
                        }
                        catch (Exception e)
                        {
                            Logger.Warn(
                                $"{GetType().Name}: Reading stream failed, disconnecting / aborted?, {e.GetType().Name}: {e.Message}");
                        }

                        if (count <= 0)
                        {
                            if (DebugEnabled)
                                Logger.Debug($"{GetType().Name} Stream read count is 0 or less. Disconnecting...");

                            Logger.Warn("{0} disconnecting!", GetType().Name);
                            OnConnectedChange(this, false);
                            try
                            {
                                _stream.Dispose();
                                _client.Dispose();
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                            }

                            _stream = null;
                            _client = null;
                            break;
                        }

                        var bytes = new byte[count];
                        Array.Copy(buffer, bytes, count);
                        if (DebugEnabled)
                            Logger.Debug(
                                $"{GetType().Name} {Address} Rx: {Tools.GetBytesAsReadableString(bytes, 0, bytes.Length, true)}");

                        OnReceivedData(this, bytes);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        break;
                    }

                if (_sendQueue != null && !_sendQueue.IsCompleted)
                {
                    _sendQueue.CompleteAdding();
                    _sendCancel.Cancel();
                }

                if (_client != null && _client.Connected)
                {
                    if (DebugEnabled) Logger.Debug($"{GetType().Name} Closing connection");

                    _client.Close();
                }
            }

            if (DebugEnabled) Logger.Debug($"{GetType().Name} exited connection process loop");

            try
            {
                _stream?.Dispose();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            _stream = null;
            _client = null;

            if (_remainConnected)
            {
                if (DebugEnabled) Logger.Debug($"{GetType().Name} reconnecting..");

                Connect();
            }
        }

        protected virtual void OnConnectedChange(IDeviceConnection client, bool connected)
        {
            Logger.Highlight($"{GetType().Name} Connected = {connected}");
            try
            {
                ConnectedChange?.Invoke(client, connected);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected virtual void OnReceivedData(IDeviceConnection client, byte[] bytes)
        {
            try
            {
                ReceivedData?.Invoke(client, bytes);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}