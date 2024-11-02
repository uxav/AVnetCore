using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Models;
using Logger = UXAV.Logging.Logger;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public class Ch5ConnectionInstance
    {
        private readonly Ch5ApiHandlerBase _apiHandler;
        private readonly Core3ControllerBase _controller;
        private readonly Mutex _sendMutex = new Mutex();
        private WebSocket _webSocket;

        public Ch5ConnectionInstance(Ch5ApiHandlerBase apiHandle)
        {
            this.ID = Guid.NewGuid().ToString();
            _apiHandler = apiHandle;
            _apiHandler.SendEvent += OnHandlerSendRequest;
            _apiHandler.SendDataEvent += OnHandlerSendDataRequest;
        }

        public Ch5ConnectionInstance(Ch5ApiHandlerBase apiHandler, Core3ControllerBase controller)
            : this(apiHandler)
        {
            _controller = controller;
            _controller.NotifyWebsocket += ControllerOnNotifyWebsocket;
        }

        public IPAddress RemoteIpAddress { get; private set; }

        public string ID { get; private set; }
        private void ControllerOnNotifyWebsocket(object sender, NotifyWebsocketEventArgs args)
        {
            _apiHandler.SendNotificationInternal(args.Method, args.Data);
        }

        public async Task RunAsync(WebSocket webSocket, Microsoft.AspNetCore.Http.HttpContext context)
        {
            _webSocket = webSocket;
            RemoteIpAddress = context.Connection.RemoteIpAddress.MapToIPv4();
            Logger.Success($"👍🏻 Websocket Opened from {RemoteIpAddress}");
            Logger.Log("Connection User-Agent:\r\n" + context.Request.Headers["User-Agent"]);

            _apiHandler.OnConnectInternal(this);
            EventService.Notify(EventMessageType.DeviceConnectionChange, new
            {
                Device = "CH5 Websocket",
                Description = $"CH5 Handler: {_apiHandler.GetType().Name}",
                ConnectionInfo = RemoteIpAddress.ToString(),
                Online = true
            });

            try
            {
                // await Task.WhenAll(ReceiveAsync(), CheckConnectionAsync());
                await Task.WhenAll(ReceiveAsync());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            Logger.Log($"👋 Websocket Closed, Reason: {_webSocket.CloseStatus}, Remote IP: {RemoteIpAddress}");

            _apiHandler.SendEvent -= OnHandlerSendRequest;
            _apiHandler.SendDataEvent -= OnHandlerSendDataRequest;
            if (_controller != null)
                _controller.NotifyWebsocket -= ControllerOnNotifyWebsocket;

            _apiHandler.OnDisconnectInternal(this);

            EventService.Notify(EventMessageType.DeviceConnectionChange, new
            {
                Device = "CH5 Websocket",
                Description = $"CH5 Handler: {_apiHandler.GetType().Name}",
                ConnectionInfo = RemoteIpAddress.ToString(),
                Online = false
            });
        }

        private async Task ReceiveAsync()
        {
            var buffer = new byte[1024];
            _webSocket = _webSocket ?? throw new NullReferenceException("WebSocket is null");
            while (_webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var data = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        try
                        {
                            _apiHandler.OnReceiveInternal(JToken.Parse(data));
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        //Logger.Debug($"🟠 WS received from {RemoteIpAddress}:\r\n" +
                        //             Tools.GetBytesAsReadableString(buffer, 0, result.Count, true));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
        }

        private async Task CheckConnectionAsync()
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                // try
                // {
                //     await _webSocket.SendAsync(new ArraySegment<byte>([]), WebSocketMessageType.Text, true,
                //         CancellationToken.None);
                // }
                // catch (Exception e)
                // {
                //     Logger.Error(e);
                //     break;
                // }
                await Task.Delay(5000);
            }
        }

        private void OnHandlerSendRequest(string data)
        {
            _sendMutex.WaitOne();
            try
            {
                //Logger.Debug($"🟢 WS send to {RemoteIpAddress}:\r\n" + data);
                var bytes = System.Text.Encoding.UTF8.GetBytes(data);
                _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            _sendMutex.ReleaseMutex();
        }

        private void OnHandlerSendDataRequest(byte[] data)
        {
            _sendMutex.WaitOne();
            try
            {
                //Logger.Debug($"🟢 WS send to {RemoteIpAddress}:\r\n" + data);
                _webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            _sendMutex.ReleaseMutex();
        }
    }
}