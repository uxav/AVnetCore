using System.Collections.Generic;
using UXAV.AVnet.Core.Models;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class Ch5StatusApiHandler : ApiRequestHandler
    {
        public Ch5StatusApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request, true)
        {
        }

        public void Get()
        {
            var page = Request.RoutePatternArgs["page"];
            switch (page)
            {
                case "urls":
                    var data = new List<object>();
                    var rooms = UxEnvironment.GetRooms();
                    foreach (var room in rooms)
                    {
                        data.Add(new
                        {
                            room.Id,
                            room.Name,
                            Url = room.HtmlUiUrl
                        });
                    }
                    WriteResponse(data);
                    return;
                default:
                    HandleNotFound();
                    return;
            }
        }
    }
}