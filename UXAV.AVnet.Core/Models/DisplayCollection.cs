using System.Collections.Generic;
using System.Linq;
using UXAV.AVnet.Core.Models.Collections;
using UXAV.AVnet.Core.Models.Rooms;

namespace UXAV.AVnet.Core.Models
{
    public class DisplayCollection<T> : UXCollection<T> where T : DisplayControllerBase
    {
        internal DisplayCollection()
        {
        }

        internal DisplayCollection(IEnumerable<T> fromDisplays)
            : base(fromDisplays)
        {
        }

        public DisplayCollection<T> DisplaysForRoom(RoomBase room)
        {
            return new DisplayCollection<T>(this.Where(d => d.Room == room));
        }

        public DisplayCollection<T> DisplaysNotAssignedToRoom()
        {
            return new DisplayCollection<T>(this.Where(d => d.Room == null));
        }

        public DisplayCollection<T> WithDevice()
        {
            return new DisplayCollection<T>(this.Where(d => d.Device != null));
        }
    }
}