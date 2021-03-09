using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.UI;
using UXAV.AVnetCore.DeviceSupport;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.UI;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class XPanelDetailsApiHandler : ApiRequestHandler
    {
        public XPanelDetailsApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        public void Get()
        {
            var results = new List<object>();
            var roomCounts = new Dictionary<uint, int>();
            foreach (var device in CipDevices.GetDevices()
                .Where(d => d is XpanelForSmartGraphics))
            {
                uint roomId = 0;
                var roomName = string.Empty;
                var roomCount = 0;
                if (Core3Controllers.Contains(device.ID))
                {
                    var controller = Core3Controllers.Get(device.ID);
                    roomId = controller.AllocatedRoom?.Id ?? 0;
                    roomName = controller.AllocatedRoom?.Name ?? string.Empty;
                    if (roomId > 0)
                    {
                        if (!roomCounts.ContainsKey(roomId))
                        {
                            roomCounts[roomId] = 1;
                        }
                        else
                        {
                            roomCounts[roomId]++;
                        }

                        roomCount = roomCounts[roomId];
                    }
                }

                var resourcePath = CipDevices.GetPathOfVtzFileForXPanel(device.ID);
                var link =
                    $"CrestronDesktop:https://{SystemBase.IpAddress}/cws/files/xpanels/Core3XPanel_{device.ID:X2}.c3p"
                    + $" -- overrideHost=true host={SystemBase.IpAddress} ipid={device.ID} port=41796 enableSSL=true"
                    + " SupportsSerialAppend=true bypasslogindialog=true";

                results.Add(new
                {
                    IpId = device.ID,
                    device.Description,
                    Type = device.GetType().FullName,
                    RoomId = roomId,
                    RoomName = roomName,
                    RoomPanelIndex = roomCount,
                    Resource = resourcePath,
                    Available = !string.IsNullOrEmpty(resourcePath),
                    Link = link
                });
            }

            WriteResponse(results);
        }
    }
}