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
            try
            {
                Ch5WebSocketServer.AddDeviceService(this);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            Device.StringInput[1002].StringValue = Device.ID.ToString("X2");

            Device.SigChange += (device, args) =>
            {
                if (args.Event != eSigEvent.BoolChange || args.Sig.Number != 1001 || !args.Sig.BoolValue) return;
                Logger.Log("Device received high join on 1001, sending websocket URL");
                device.StringInput[1001].StringValue = WebSocketUrl;
                device.StringInput[1002].StringValue = device.ID.ToString("X2");
            };
        }

        public string WebSocketUrl
        {
            get => _webSocketUrl;
            internal set
            {
                _webSocketUrl = value;
                Device.StringInput[1001].StringValue = _webSocketUrl;
            }
        }
    }
}