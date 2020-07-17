using System.Collections.Generic;
using UXAV.AVnetCore.Models.Collections;

namespace UXAV.AVnetCore.Models.Rooms
{
    /// <summary>
    /// Collection of <see cref="RoomBase"/> items
    /// </summary>
    public class RoomCollection<T> : UXCollection<T> where T : RoomBase
    {
        internal RoomCollection()
        {

        }

        internal RoomCollection(IEnumerable<T> fromRooms)
            : base(fromRooms)
        {

        }
    }
}