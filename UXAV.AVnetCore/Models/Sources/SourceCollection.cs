using System;
using System.Collections.Generic;
using System.Linq;
using UXAV.AVnetCore.Models.Collections;
using UXAV.AVnetCore.Models.Rooms;

namespace UXAV.AVnetCore.Models.Sources
{
    /// <summary>
    /// A collection of <see cref="SourceBase"/> items
    /// </summary>
    public class SourceCollection<T> : UXCollection<T> where T : SourceBase
    {
        internal SourceCollection()
        {

        }

        internal SourceCollection(IEnumerable<T> fromSources)
            : base(fromSources)
        {

        }

        public SourceBase this[SourceType type]
        {
            get { return this.FirstOrDefault(s => s.Type == type); }
        }

        /// <summary>
        /// Access specific <see cref="SourceType"/> types of sources
        /// </summary>
        /// <param name="type">The type of source</param>
        /// <returns>A SourceCollection of specified type</returns>
        public SourceCollection<T> SourcesOfType(SourceType type)
        {
            return new SourceCollection<T>(this.Where(s => s.Type == type));
        }

        /// <summary>
        /// Get a collection of sources assigned to a particular room
        /// </summary>
        /// <param name="room">The room to get sources for</param>
        /// <returns>A SourceCollection</returns>
        /// <exception cref="ArgumentException">Thrown if room is null</exception>
        public SourceCollection<T> SourcesForRoom(RoomBase room)
        {
            if(room == null) throw new ArgumentException("room cannot be null");
            return new SourceCollection<T>(this.Where(s => !s.IsLocalToDisplay && s.AssignedRooms.Keys.Contains(room.Id)));
        }

        /// <summary>
        /// Get a collection of sources assigned to a particular room or are global (not assigned to a room)
        /// </summary>
        /// <param name="room">The room to get sources for</param>
        /// <returns>A SourceCollection</returns>
        /// <exception cref="ArgumentException">Thrown if room is null</exception>
        public SourceCollection<T> SourcesForRoomOrGlobal(RoomBase room)
        {
            if(room == null) throw new ArgumentException("room cannot be null");
            return new SourceCollection<T>(this.Where(s =>
                !s.IsLocalToDisplay && (s.AssignedRooms.Keys.Contains(room.Id) || s.AssignedRooms.Count == 0)));
        }

        public SourceCollection<T> SourcesForDisplay(DisplayControllerBase display)
        {
            return new SourceCollection<T>(this.Where(s => s.AssignedDisplay == display));
        }

        public SourceCollection<T> SourcesOfGroupName(string groupName)
        {
            return new SourceCollection<T>(this.Where(s => s.GroupName == groupName));
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return InternalDictionary.Values
                .OrderBy(s => s.Priority)
                .ThenBy(s => s.Name)
                .ThenBy(s => s.Id)
                .GetEnumerator();
        }
    }
}