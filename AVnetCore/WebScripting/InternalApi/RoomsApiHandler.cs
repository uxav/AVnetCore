using System.Linq;
using Newtonsoft.Json.Linq;
using UXAV.AVnetCore.Models;
using UXAV.AVnetCore.Models.Rooms;

namespace UXAV.AVnetCore.WebScripting.InternalApi
{
    public class RoomsApiHandler : ApiRequestHandler
    {
        public RoomsApiHandler(WebScriptingServer server, WebScriptingRequest request)
            : base(server, request)
        {
        }

        [SecureRequest]
        public void Get()
        {
            if (Request.RoutePatternArgs.ContainsKey("id"))
            {
                var id = uint.Parse(Request.RoutePatternArgs["id"]);
                if (!UxEnvironment.GetRooms().Contains(id))
                {
                    HandleNotFound($"Room with ID: {id}, does not exist");
                    return;
                }

                if (Request.RoutePatternArgs.ContainsKey("method"))
                {
                    HandleError(400, "Bad Request", "Use post for room methods");
                    return;
                }

                WriteResponse(GetRoomObject(UxEnvironment.GetRoom(id)));
                return;
            }

            var rooms = UxEnvironment.GetRooms().Select(GetRoomObject);

            WriteResponse(rooms);
        }

        [SecureRequest]
        public void Post()
        {
            if (!Request.RoutePatternArgs.ContainsKey("id") || !Request.RoutePatternArgs.ContainsKey("method"))
            {
                HandleError(400, "Bad Request", "Invalid request url");
                return;
            }

            var id = uint.Parse(Request.RoutePatternArgs["id"]);
            if (!UxEnvironment.GetRooms().Contains(id))
            {
                HandleNotFound($"Room with ID: {id}, does not exist");
                return;
            }

            var room = UxEnvironment.GetRoom(id);
            var method = Request.RoutePatternArgs["method"];
            var json = JToken.Parse(Request.GetStringContents());
            switch (method)
            {
                case "power":
                    var power = json["value"].Value<bool>();
                    var result = room.SetPower(power);
                    WriteResponse(result);
                    return;
                default:
                    HandleError(400, "Bad Request", $"No room method named: {method}");
                    break;
            }
        }

        private static object GetRoomObject(RoomBase room)
        {
            return new
            {
                room.Id,
                room.Name,
                room.ScreenName,
                @RoomType = room.GetType().Name,
                Parent = room.ParentRoom?.Id ?? 0,
                SlaveRooms = room.ChildRooms.Keys,
                room.Power,
                @Source = room.CurrentSource?.Id ?? 0,
                @Volume = 50,
                @VolumeMute = false,
                @MicMute = false
            };
        }
    }
}