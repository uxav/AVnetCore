using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;

namespace UXAV.AVnet.Core
{
    public static class AutoDiscovery
    {
        public static AutoDiscoveryResult[] Get()
        {
            var discovery = EthernetAutodiscovery.Query();
            if (discovery != EthernetAutodiscovery.eAutoDiscoveryErrors.AutoDiscoveryOperationSuccess)
            {
                throw new OperationCanceledException($"Query result was not successull, {discovery}");
            }

            return EthernetAutodiscovery.DiscoveredElementsList.Select(element => new AutoDiscoveryResult(element))
                .OrderBy(e => e.Model)
                .ThenBy(e => e.Hostname)
                .ToArray();
        }
    }
}