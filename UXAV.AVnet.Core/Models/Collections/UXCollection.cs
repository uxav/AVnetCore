using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace UXAV.AVnet.Core.Models.Collections
{
    public class UXCollection<T> : IEnumerable<T> where T : IGenericItem
    {
        internal readonly ConcurrentDictionary<uint, T> InternalDictionary = new ConcurrentDictionary<uint, T>();

        internal UXCollection()
        {
        }

        internal UXCollection(IEnumerable<T> fromCollection)
        {
            InternalDictionary = new ConcurrentDictionary<uint, T>();
            foreach (var item in fromCollection)
            {
                if (InternalDictionary.ContainsKey(item.Id)) throw new Exception("Items contain multiple of same Id");

                InternalDictionary[item.Id] = item;
            }
        }

        /// <summary>
        ///     Get an item by it's ID - <see cref="IGenericItem.Id" />
        /// </summary>
        /// <param name="id"></param>
        public T this[uint id] => InternalDictionary[id];

        public ICollection<uint> Keys => InternalDictionary.Keys;

        public int Count => InternalDictionary.Count;

        public virtual IEnumerator<T> GetEnumerator()
        {
            return InternalDictionary.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool HasItemWithId(uint id)
        {
            return InternalDictionary.ContainsKey(id);
        }

        internal void Add(T item)
        {
            if (item == null) throw new ArgumentException("item cannot be null");

            if (InternalDictionary.ContainsKey(item.Id))
                throw new ArgumentException("Collection already contains item with same Id", nameof(item));

            InternalDictionary[item.Id] = item;
        }

        internal void Remove(T item)
        {
            if (item == null) throw new ArgumentException("item cannot be null");

            if (InternalDictionary.ContainsKey(item.Id)) InternalDictionary.TryRemove(item.Id, out _);
        }

        public bool Contains(uint id)
        {
            return InternalDictionary.ContainsKey(id);
        }
    }
}