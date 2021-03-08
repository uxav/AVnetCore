using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.UI;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.UI;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class XPanelDetailsHandler : ApiRequestHandler
    {
        public XPanelDetailsHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        public void Get()
        {
            var results = new List<object>();
            foreach (var device in CipDevices.GetDevices()
                .Where(d => d is XpanelForSmartGraphics))
            {
                uint roomId = 0;
                var roomName = string.Empty;
                if (Core3Controllers.Contains(device.ID))
                {
                    var controller = Core3Controllers.Get(device.ID);
                    roomId = controller.AllocatedRoom?.Id ?? 0;
                    roomName = controller.AllocatedRoom?.Name ?? string.Empty;
                }

                var resourcePath = CipDevices.GetPathOfVtzFileForXPanel(device.ID);
                var link =
                    $"CrestronDesktop:https://{SystemBase.IpAddress}/cws/files/xpanels/Core3XPanel_{device.ID:X2}.c3p"
                    + $" -- overrideHost=true ipid={device.ID:x2} port=41794 enableSSL=false";

                results.Add(new
                {
                    IpId = device.ID,
                    device.Description,
                    Type = device.GetType().FullName,
                    RoomId = roomId,
                    RoomName = roomName,
                    Resource = resourcePath,
                    Available = !string.IsNullOrEmpty(resourcePath),
                    Link = link
                });
            }

            WriteResponse(results);
        }
    }
}