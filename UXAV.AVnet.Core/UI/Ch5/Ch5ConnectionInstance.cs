using System;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Models;
using WebSocketSharp;
using WebSocketSharp.Server;
using Logger = UXAV.Logging.Logger;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public class Ch5ConnectionInstance : WebSocketBehavior
    {
        private readonly Ch5ApiHandlerBase _apiHandler;
        private readonly Core3ControllerBase _controller;
        private readonly Mutex _sendMutex = new Mutex();

        public Ch5ConnectionInstance(Ch5ApiHandlerBase apiHandler)
        {
            _apiHandler = apiHandler;
            _apiHandler.SendEvent += OnHandlerSendRequest;
        }

        public Ch5ConnectionInstance(Ch5ApiHandlerBase apiHandler, Core3ControllerBase controller)
        {
            _apiHandler = apiHandler;
            _apiHandler.SendEvent += OnHandlerSendRequest;
            _controller = controller;
            _controller.NotifyWebsocket += ControllerOnNotifyWebsocket;
        }

        public IPAddress RemoteIpAddress { get; private set; }

        private void ControllerOnNotifyWebsocket(object sender, NotifyWebsocketEventArgs args)
        {
            _apiHandler.SendNotificationInternal(args.Method, args.Data);
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            RemoteIpAddress = Context.UserEndPoint.Address;
            Logger.Success($"👍🏻 Websocket Opened from {RemoteIpAddress}, ID = \"{ID}\"");
            Logger.Log("Connection User-Agent:\r\n" + Context.Headers["User-Agent"]);
            foreach (var protocol in Context.SecWebSocketProtocols)
            {
                //Logger.Debug($"Connection protocol includes: {protocol}");
            }

            _apiHandler.OnConnectInternal(this);
            EventService.Notify(EventMessageType.DeviceConnectionChange, new
            {
                Device = "CH5 Websocket",
                Description = $"CH5 Handler: {_apiHandler.GetType().Name}",
                ConnectionInfo = RemoteIpAddress.ToString(),
                Online = true
            });
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            Logger.Log($"👋 Websocket Closed, {e.Code}, Clean: {e.WasClean}, Remote IP: {RemoteIpAddress}");
            _apiHandler.SendEvent -= OnHandlerSendRequest;
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

        protected override void OnError(ErrorEventArgs e)
        {
            base.OnError(e);
            Logger.Error(e.Exception);
        }

        protected override void OnMessage(MessageEventArgs args)
        {
            base.OnMessage(args);
            try
            {
                if (args.IsPing)
                {
                    //Logger.Debug("Websocket received Ping!");
                }
                else if (args.IsBinary)
                {
                    /*Logger.Debug($"🟠 WS received from {RemoteIpAddress}:\r\n" +
                                 Tools.GetBytesAsReadableString(args.RawData, 0, args.RawData.Length, true));*/
                }
                else if (args.IsText)
                {
                    var data = args.Data;
                    if (data != null)
                        //Logger.Debug($"🟠 WS received from {RemoteIpAddress}:\r\n" + data);
                        try
                        {
                            _apiHandler.OnReceiveInternal(JToken.Parse(data));
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                        }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void OnHandlerSendRequest(string data)
        {
            if (State != WebSocketState.Open) return;
            _sendMutex.WaitOne();
            try
            {
                //Logger.Debug($"🟢 WS send to {RemoteIpAddress}:\r\n" + data);
                Send(data);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            _sendMutex.ReleaseMutex();
        }
    }
}