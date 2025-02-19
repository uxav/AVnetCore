using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UXAV.AVnet.Core.Models;

namespace UXAV.AVnet.Core.WebScripting.InternalApi
{
    internal class SourcesApiHandler : ApiRequestHandler
    {
        public SourcesApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        [SecureRequest]
        public async void Get()
        {
            var result = new List<object>();

            var sources = UxEnvironment.GetSources();

            if (!string.IsNullOrEmpty(Request.Query["room"]))
                try
                {
                    var id = uint.Parse(Request.Query["room"]);
                    sources = sources.SourcesForRoom(UxEnvironment.GetRoom(id));
                }
                catch (Exception e)
                {
                    await HandleErrorAsync(e);
                    return;
                }

            foreach (var source in sources)
                result.Add(new
                {
                    source.Id,
                    source.Name,
                    source.GroupName,
                    source.Type,
                    source.IconName,
                    source.Priority,
                    AssignedRooms = source.AssignedRooms.Keys,
                    ActiveRooms = UxEnvironment.GetRooms()
                        .Where(r => r.GetCurrentSource() != null && r.GetCurrentSource().Id == source.Id)
                        .Select(r => r.Id)
                });

            await WriteResponseAsync(result);
        }

        [SecureRequest]
        public async void Post()
        {
            var json = JToken.Parse(await Request.GetStringContentsAsync());

            var roomId = json["RoomId"].Value<uint>();
            var sourceId = json["SourceId"].Value<uint>();

            var result = UxEnvironment.GetRooms()[roomId]
                .SelectSourceAsync(sourceId > 0 ? UxEnvironment.GetSources()[sourceId] : null);

            await WriteResponseAsync(result);
        }
    }
}