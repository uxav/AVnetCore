using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UXAV.AVnetCore.Models;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class SourcesApiHandler : ApiRequestHandler
    {
        public SourcesApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {

        }

        [SecureRequest]
        public void Get()
        {
            var result = new List<object>();

            var sources = UxEnvironment.GetSources();

            if (!string.IsNullOrEmpty(Request.Query["room"]))
            {
                try
                {
                    var id = uint.Parse(Request.Query["room"]);
                    sources = sources.SourcesForRoom(UxEnvironment.GetRoom(id));
                }
                catch (Exception e)
                {
                    HandleError(e);
                    return;
                }
            }

            foreach (var source in sources)
            {
                result.Add(new
                {
                    source.Id,
                    source.Name,
                    source.GroupName,
                    source.Type,
                    source.IconName,
                    source.Priority,
                    @AssignedRooms = source.AssignedRooms.Keys,
                    @ActiveRooms = UxEnvironment.GetRooms()
                        .Where(r => r.CurrentSource != null && r.CurrentSource.Id == source.Id)
                        .Select(r => r.Id)
                });
            }

            WriteResponse(result);
        }

        [SecureRequest]
        public void Post()
        {
            var json = JToken.Parse(Request.GetStringContents());

            var roomId = json["RoomId"].Value<uint>();
            var sourceId = json["SourceId"].Value<uint>();

            var result = UxEnvironment.GetRooms()[roomId]
                .SelectSource(sourceId > 0 ? UxEnvironment.GetSources()[sourceId] : null);

            WriteResponse(result);
        }
    }
}