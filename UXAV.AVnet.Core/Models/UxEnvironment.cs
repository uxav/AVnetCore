using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Crestron.SimplSharpPro;
using UXAV.AVnet.Core.Models.Rooms;
using UXAV.AVnet.Core.Models.Sources;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Models
{
    /// <summary>
    ///     Static method to gain access to all Sources in the program
    /// </summary>
    public static class UxEnvironment
    {
        private static readonly SourceCollection<SourceBase> SourceCollection = new SourceCollection<SourceBase>();
        private static readonly RoomCollection<RoomBase> RoomsCollection = new RoomCollection<RoomBase>();

        private static readonly DisplayCollection<DisplayControllerBase> DisplayCollection =
            new DisplayCollection<DisplayControllerBase>();

        private static string _version;
        private static string _productVersion;
        private static string _assemblyVersion;

        static UxEnvironment()
        {
            var assembly = Assembly.GetExecutingAssembly();
            Name = assembly.GetName().Name;
        }

        public static SystemBase System { get; internal set; }
        public static CrestronControlSystem ControlSystem { get; internal set; }
        public static string Name { get; }
        public static string AssemblyVersion
        {
            get
            {
                if (_assemblyVersion != null) return _assemblyVersion;
                var assembly = Assembly.GetExecutingAssembly();
                _assemblyVersion = assembly.GetName().Version.ToString();

                return _assemblyVersion;
            }
        }

        public static string Version
        {
            get
            {
                if (_version != null) return _version;
                var vi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                var r = new Regex(@"^(\d+\.\d+\.\d+[^+]*)");
                if (r.IsMatch(vi.ProductVersion))
                    _version = r.Match(vi.ProductVersion).Groups[1].Value;
                else
                    // If the version is not in the format x.x.x, just use the product version (which is the file version
                    _version = $"{vi.ProductMajorPart}.{vi.ProductMinorPart}.{vi.ProductBuildPart}";

                return _version;
            }
        }

        public static string ProductVersion
        {
            get
            {
                if (_productVersion != null) return _productVersion;
                var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                _productVersion = versionInfo.ProductVersion;

                return _productVersion;
            }
        }

        internal static void InitConsoleCommands()
        {
            Logger.AddCommand((argString, args, connection, respond) =>
            {
                foreach (var room in GetRooms()) respond(room + "\r\n");
            }, "ListRooms", "List all rooms");
            Logger.AddCommand((argString, args, connection, respond) =>
            {
                foreach (var source in GetSources()) respond(source + "\r\n");
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

        internal static void AddDisplay(DisplayControllerBase display)
        {
            DisplayCollection.Add(display);
            Logger.Log($"Added display {display.Id} to collection");
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

        public static RoomBase GetRoom(string withUniqueId)
        {
            return RoomsCollection.FirstOrDefault(s => s.UniqueId == withUniqueId);
        }

        public static T GetRoom<T>(uint id) where T : RoomBase
        {
            return RoomsCollection[id] as T;
        }

        public static RoomCollection<T> GetRooms<T>() where T : RoomBase
        {
            return new RoomCollection<T>(RoomsCollection.Where(r => r is T).Cast<T>());
        }

        public static T GetSource<T>(uint sourceId) where T : SourceBase
        {
            return !SourceCollection.HasItemWithId(sourceId) ? null : SourceCollection[sourceId] as T;
        }

        public static SourceBase GetSource(uint sourceId)
        {
            return !SourceCollection.HasItemWithId(sourceId) ? null : SourceCollection[sourceId];
        }

        public static SourceBase GetSource(string withUniqueId)
        {
            return SourceCollection.FirstOrDefault(s => s.UniqueId == withUniqueId);
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
            return new SourceCollection<T>(SourceCollection.Where(s => s is T).Cast<T>());
        }

        public static bool DisplayWithIdExists(uint id)
        {
            return DisplayCollection.HasItemWithId(id);
        }

        public static DisplayControllerBase GetDisplay(uint displayId)
        {
            return !DisplayCollection.HasItemWithId(displayId) ? null : DisplayCollection[displayId];
        }

        public static DisplayControllerBase GetDisplay(string withUniqueId)
        {
            return DisplayCollection.FirstOrDefault(d => d.UniqueId == withUniqueId);
        }

        public static DisplayCollection<DisplayControllerBase> GetDisplays()
        {
            return DisplayCollection;
        }
    }
}