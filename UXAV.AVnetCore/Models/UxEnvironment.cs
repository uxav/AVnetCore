using System;
using System.Linq;
using Crestron.SimplSharpPro;
using UXAV.AVnetCore.Models.Rooms;
using UXAV.AVnetCore.Models.Sources;
using UXAV.Logging;

namespace UXAV.AVnetCore.Models
{
    /// <summary>
    /// Static method to gain access to all Sources in the program
    /// </summary>
    public static class UxEnvironment
    {
        private static readonly SourceCollection<SourceBase> SourceCollection = new SourceCollection<SourceBase>();
        private static readonly RoomCollection<RoomBase> RoomsCollection = new RoomCollection<RoomBase>();

        internal static void InitConsoleCommands()
        {
            Logger.AddCommand((argString, args, connection, respond) =>
            {
                foreach (var room in GetRooms())
                {
                    respond(room + "\r\n");
                }
            }, "ListRooms", "List all rooms");
            Logger.AddCommand((argString, args, connection, respond) =>
            {
                foreach (var source in GetSources())
                {
                    respond(source + "\r\n");
                }
            }, "ListSources", "List all sources");
            Logger.AddCommand((argString, args, connection, respond) =>
            {
                try
                {
                    var roomId = uint.Parse(args["room"]);
                    var sourceId = uint.Parse(args["source"]);
                    GetRoom(roomId).SelectSource(GetSource(sourceId));
                }
                catch (Exception e)
                {
                    respond(e.ToString());
                }
            }, "SelectSource", "Select source in room", "room", "source");
        }

        internal static void AddRoom(RoomBase room)
        {
            RoomsCollection.Add(room);
        }

        internal static void AddSource(SourceBase source)
        {
            SourceCollection.Add(source);
            Logger.Log($"Added source {source.Id} to collection");
        }

        public static SystemBase System { get; internal set; }

        public static CrestronControlSystem ControlSystem { get; internal set; }

        public static bool RoomWithIdExists(uint id)
        {
            return RoomsCollection.Contains(id);
        }

        public static RoomCollection<RoomBase> GetRooms()
        {
            return RoomsCollection;
        }

        public static RoomBase GetRoom(uint id)
        {
            return RoomsCollection[id];
        }

        public static T GetRoom<T>(uint id) where T : RoomBase
        {
            return RoomsCollection[id] as T;
        }

        public static RoomCollection<T> GetRooms<T>() where T : RoomBase
        {
            return new RoomCollection<T>(RoomsCollection.Cast<T>());
        }

        public static SourceBase GetSource(uint sourceId)
        {
            return !SourceCollection.HasItemWithId(sourceId) ? null : SourceCollection[sourceId];
        }

        public static SourceCollection<SourceBase> GetSources()
        {
            return SourceCollection;
        }

        public static SourceCollection<T> GetSources<T>() where T : SourceBase
        {
            return new SourceCollection<T>(SourceCollection.Cast<T>());
        }
    }
}