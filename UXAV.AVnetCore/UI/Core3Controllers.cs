using System;
using System.Collections.Generic;
using System.Linq;
using UXAV.AVnetCore.Models.Rooms;

namespace UXAV.AVnetCore.UI
{
    public static class Core3Controllers
    {
        private static readonly Dictionary<uint, Core3ControllerBase> Controllers =
            new Dictionary<uint, Core3ControllerBase>();

        internal static void Add(uint id, Core3ControllerBase controller)
        {
            lock (Controllers)
            {
                if (Controllers.ContainsKey(id))
                {
                    throw new ArgumentException("Collection already contains controller with ID " + id);
                }

                Controllers.Add(id, controller);
            }
        }

        public static Core3ControllerBase[] Get()
        {
            lock (Controllers)
            {
                return Controllers.Values.ToArray();
            }
        }

        public static T[] Get<T>() where T : Core3ControllerBase
        {
            lock (Controllers)
            {
                return (from controller in Controllers.Values
                    let c = controller as T
                    where c != null
                    select c).ToArray();
            }
        }

        public static Core3ControllerBase Get(uint ipId)
        {
            lock (Controllers)
            {
                return Controllers[ipId];
            }
        }

        public static Core3ControllerBase Get<T>(uint ipId) where T : Core3ControllerBase
        {
            lock (Controllers)
            {
                return (T) Controllers[ipId];
            }
        }

        public static Core3ControllerBase[] GetCore3Controllers(this RoomBase room)
        {
            lock (Controllers)
            {
                return Controllers.Values.Where(controller => controller.Room == room).ToArray();
            }
        }

        public static T[] GetCore3Controllers<T>(this RoomBase room) where T : Core3ControllerBase
        {
            lock (Controllers)
            {
                return (from controller in Controllers.Values
                    let c = controller as T
                    where c != null && controller.Room == room
                    select c).ToArray();
            }
        }
    }
}