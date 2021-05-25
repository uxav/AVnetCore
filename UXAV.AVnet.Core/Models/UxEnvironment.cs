using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Crestron.SimplSharpPro;
using UXAV.AVnet.Core.Models.Rooms;
using UXAV.AVnet.Core.Models.Sources;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Models
{
    /// <summary>
    /// Static method to gain access to all Sources in the program
    /// </summary>
    public static class UxEnvironment
    {
        private static readonly SourceCollection<SourceBase> SourceCollection = new SourceCollection<SourceBase>();
        private static readonly RoomCollection<RoomBase> RoomsCollection = new RoomCollection<RoomBase>();
        private static Version _version;

        static UxEnvironment()
        {
            var assembly = Assembly.GetExecutingAssembly();
            Name = assembly.GetName().Name;
            AssemblyVersion = assembly.GetName().Version;
        }

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
            Logger.AddCommand(async (argString, args, connection, respond) =>
            {
                try
                {
                    var roomId = uint.Parse(args["room"]);
                    var sourceId = uint.Parse(args["source"]);
                    await GetRoom(roomId).SelectSourceAsync(GetSource(sourceId));
                }
                catch (Exception e)
                {
                    respond(e.ToString());
                }
            }, "SelectSource", "Select source in room", "room", "source");
            Logger.AddCommand((argString, args, connection, respond) =>
            {
                try
                {
                    var roomId = uint.Parse(args["room"]);
                    GetRoom(roomId).PowerOff();
                }
                catch (Exception e)
                {
                    respond(e.ToString());
                }
            }, "PowerOffRoom", "Shutdown room", "room");
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
        public static string Name { get; }
        public static Version AssemblyVersion { get; }

        public static Version Version
        {
            get
            {
                if (_version != null) return _version;
                var assembly = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                _version = new Version(assembly.FileVersion);

                return _version;
            }
        }

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

        public static bool SourcesContainSourceWithId(uint id)
        {
            return SourceCollection.HasItemWithId(id);
        }

        public static SourceCollection<T> GetSources<T>() where T : SourceBase
        {
            return new SourceCollection<T>(SourceCollection.Cast<T>());
        }
    }
}