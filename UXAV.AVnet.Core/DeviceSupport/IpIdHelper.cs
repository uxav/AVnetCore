using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace UXAV.AVnet.Core.DeviceSupport
{
    public class IpIdHelper
    {
        private const uint StartId = 0x03;
        private const uint MaxId = 0xFE;
        private readonly ConcurrentBag<uint> _usedValues;

        public IpIdHelper()
        {
            _usedValues = new ConcurrentBag<uint>();
        }

        public IpIdHelper(IEnumerable<uint> usedIpIds)
        {
            _usedValues = new ConcurrentBag<uint>(usedIpIds);
        }

        public static IpIdHelper GetFromSystemAvailability()
        {
            return new IpIdHelper(CipDevices.GetUsedIpIds());
        }

        public uint GetNextValue()
        {
            return GetNextValueStartingAt(StartId);
        }

        public uint GetNextValueStartingAt(uint ipId)
        {
            if (ipId < 0x03) throw new IndexOutOfRangeException("id must be greater than 0x03");
            for (var id = ipId; id <= MaxId; id++)
            {
                if (_usedValues.Contains(id)) continue;
                _usedValues.Add(id);
                return id;
            }

            throw new InvalidOperationException("No more ID's available");
        }
    }
}