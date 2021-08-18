using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.UI;
using UXAV.AVnet.Core.Config;
using UXAV.AVnet.Core.DeviceSupport;
using UXAV.AVnet.Core.Models;
using UXAV.AVnet.Core.UI;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
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
                var ssl = ConfigManager.GetOrCreatePropertyListItem("xpanelsslmode", false);
                var port = ConfigManager.GetOrCreatePropertyListItem("xpanelport", 41794);
                var baseUrl = string.Empty;

                baseUrl = CrestronEnvironment.DevicePlatform == eDevicePlatform.Server
                    ? $"http://{SystemBase.IpAddress}{SystemBase.CwsPath}"
                    : $"https://{SystemBase.IpAddress}{SystemBase.CwsPath}";
                var link =
                    $"CrestronDesktop:{baseUrl}/files/xpanels/Core3XPanel_{device.ID:X2}.c3p"
                    + $" -- overrideHost=true host={SystemBase.IpAddress} ipid={device.ID} port={port} enableSSL={ssl}"
                    + " SupportsSerialAppend=true bypasslogindialog=true";
                if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)
                {
                    link += $" programInstanceId={InitialParametersClass.RoomId}";
                }

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