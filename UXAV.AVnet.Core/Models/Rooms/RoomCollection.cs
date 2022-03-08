using System.Collections.Generic;
using UXAV.AVnet.Core.Models.Collections;

namespace UXAV.AVnet.Core.Models.Rooms
{
    /// <summary>
    ///     Collection of <see cref="RoomBase" /> items
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