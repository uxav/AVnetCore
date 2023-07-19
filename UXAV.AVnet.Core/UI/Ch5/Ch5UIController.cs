using System;
using Crestron.SimplSharpPro;
using UXAV.AVnet.Core.Models;
using UXAV.Logging;

namespace UXAV.AVnet.Core.UI.Ch5
{
    public abstract class Ch5UIController<THandler> : Core3ControllerBase where THandler : Ch5ApiHandlerBase
    {
        private string _webSocketUrl;

        protected Ch5UIController(SystemBase system, uint roomId, string typeName, uint ipId, string description,
            string pathOfVtz)
            : base(system, roomId, typeName, ipId, description, pathOfVtz)
        {
            Device.StringInput[11].StringValue = Device.ID.ToString("X2");

            Device.SigChange += (device, args) =>
            {
                if (args.Event == eSigEvent.StringChange && args.Sig.Number == 12)
                {
                    Logger.Log($"Received log over CIP from Device {device}: {args.Sig.StringValue}");
                    return;
                }

                if (args.Event != eSigEvent.BoolChange || args.Sig.Number != 10 || !args.Sig.BoolValue) return;
                Logger.Log($"Device received high join on 10, sending websocket URL: {WebSocketUrl}");
                device.StringInput[10].StringValue = WebSocketUrl;
                device.StringInput[11].StringValue = device.ID.ToString("X2");
            };
        }

        public string WebSocketUrl
        {
            get => _webSocketUrl;
            internal set
            {
                _webSocketUrl = value;
                Device.StringInput[10].StringValue = _webSocketUrl;
            }
        }

        protected override void OnOnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            base.OnOnlineStatusChange(currentDevice, args);
            Logger.Log("Device online, sending websocket URL");
            Device.StringInput[10].StringValue = WebSocketUrl;
            Device.StringInput[11].StringValue = Device.ID.ToString("X2");
        }

        internal override void InitializeInternal()
        {
            try
            {
                // ReSharper disable once RedundantTypeArgumentsOfMethod
                Ch5WebSocketServer.AddDeviceService<THandler>(this);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            base.InitializeInternal();
        }
    }
}