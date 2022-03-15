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
        private readonly Mutex _sendMutex = new Mutex();

        public Ch5ConnectionInstance(Ch5ApiHandlerBase apiHandler)
        {
            _apiHandler = apiHandler;
            _apiHandler.SendEvent += OnHandlerSendRequest;
        }

        public IPAddress RemoteIpAddress { get; private set; }

        protected override void OnOpen()
        {
            base.OnOpen();
            RemoteIpAddress = Context.UserEndPoint.Address;
            Logger.Success($"Websocket Opened from {RemoteIpAddress}!");
            foreach (var protocol in Context.SecWebSocketProtocols)
                Logger.Debug($"Connection protocol includes: {protocol}");
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
            Logger.Warn($"Websocket Closed, {e.Code}, Clean: {e.WasClean}, Remote IP: {RemoteIpAddress}");
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
                    Logger.Debug($"Received from websocket at {RemoteIpAddress}:\r\n" +
                                 Tools.GetBytesAsReadableString(args.RawData, 0, args.RawData.Length, true));
                }
                else if (args.IsText)
                {
                    var data = args.Data;
                    if (data != null)
                    {
                        Logger.Debug($"Received from websocket at {RemoteIpAddress}:\r\n" + data);
                        _apiHandler.OnReceiveInternal(JToken.Parse(data));
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
            _sendMutex.WaitOne();
            Send(data);
            _sendMutex.ReleaseMutex();
        }
    }
}